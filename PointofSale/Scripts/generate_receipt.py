"""
generate_receipt.py
Called by ReceiptPdfService.cs via Process.Start.
Usage: python generate_receipt.py <json_data_file> <output_pdf_path>
"""

import sys, json, os
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.units import mm
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, HRFlowable
)
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_LEFT, TA_RIGHT, TA_CENTER
from reportlab.pdfgen import canvas as pdfcanvas

# ── Helpers ───────────────────────────────────────────────────────────────────

def money(val):
    try:    return f"R{float(val):,.2f}"
    except: return str(val)

def style(name, **kwargs):
    base = {"fontName": "Helvetica", "fontSize": 10, "leading": 14, "textColor": colors.black}
    base.update(kwargs)
    return ParagraphStyle(name, **base)

# ── Styles ────────────────────────────────────────────────────────────────────

S_STORE_NAME  = style("storename", fontSize=18, fontName="Helvetica-Bold")
S_STORE_INFO  = style("storeinfo", fontSize=9,  textColor=colors.HexColor("#555555"))
S_RECEIPT_LBL = style("reclbl",    fontSize=22, fontName="Helvetica-Bold", alignment=TA_RIGHT)
S_META_LBL    = style("metalbl",   fontSize=9,  alignment=TA_RIGHT, textColor=colors.HexColor("#555555"))
S_META_VAL    = style("metaval",   fontSize=9,  alignment=TA_RIGHT, fontName="Helvetica-Bold")
S_NORMAL      = style("normal",    fontSize=9)
S_BOLD        = style("bold",      fontSize=9,  fontName="Helvetica-Bold")
S_FOOTER      = style("footer",    fontSize=8,  textColor=colors.HexColor("#777777"))

LIGHT     = colors.HexColor("#f5f5f5")
BORDER    = colors.HexColor("#cccccc")
HEADER_BG = colors.HexColor("#222222")

# ── Stamp canvas ──────────────────────────────────────────────────────────────

def make_stamp_canvas_factory(stamp_path, stamp_text):
    """Returns a canvas class that draws a stamp on every page."""

    class StampCanvas(pdfcanvas.Canvas):
        def __init__(self, filename, **kwargs):
            super().__init__(filename, **kwargs)
            self._stamp_path = stamp_path
            self._stamp_text = stamp_text

        def showPage(self):
            self._draw_stamp()
            super().showPage()

        def _draw_stamp(self):
            W, H = A4
            self.saveState()
            self.translate(W / 2, H / 2)
            self.rotate(35)

            # Image stamp
            if self._stamp_path and os.path.isfile(self._stamp_path):
                try:
                    from reportlab.lib.utils import ImageReader
                    img   = ImageReader(self._stamp_path)
                    iw, ih = img.getSize()
                    max_w  = 80 * mm
                    scale  = min(max_w / iw, max_w / ih)
                    dw, dh = iw * scale, ih * scale
                    self.setFillAlpha(0.20)
                    self.drawImage(self._stamp_path, -dw/2, -dh/2, dw, dh,
                                   mask='auto', preserveAspectRatio=True)
                except Exception:
                    pass

            # Text stamp
            elif self._stamp_text:
                text = self._stamp_text.upper()
                fs   = 72
                tw   = self.stringWidth(text, "Helvetica-Bold", fs)
                self.setFont("Helvetica-Bold", fs)
                self.setFillColorRGB(0.15, 0.50, 0.15)
                self.setStrokeColorRGB(0.15, 0.50, 0.15)
                self.setFillAlpha(0.15)
                self.setStrokeAlpha(0.25)
                self.setLineWidth(2)
                pad = 12
                self.roundRect(-tw/2 - pad, -fs/2 - pad/2, tw + pad*2, fs + pad,
                               8, stroke=1, fill=0)
                self.drawCentredString(0, -fs/2 + 10, text)

            self.restoreState()

    return StampCanvas


# ── Main build ────────────────────────────────────────────────────────────────

def build(data: dict, out_path: str):
    logo_path  = data.get("LogoPath",  "") or ""
    stamp_path = data.get("StampPath", "") or ""
    stamp_text = data.get("StampText", "") or ""

    has_stamp = (stamp_path and os.path.isfile(stamp_path)) or bool(stamp_text)
    canvasmaker = make_stamp_canvas_factory(stamp_path, stamp_text) if has_stamp else None

    doc = SimpleDocTemplate(
        out_path,
        pagesize=A4,
        leftMargin=18*mm, rightMargin=18*mm,
        topMargin=18*mm,  bottomMargin=18*mm,
    )

    W = A4[0] - 36*mm
    story = []

    # ── Header: Logo or store name left, RECEIPT right ────────────────────
    if logo_path and os.path.isfile(logo_path):
        try:
            from reportlab.platypus import Image as RLImage
            logo_img = RLImage(logo_path, width=55*mm, height=22*mm, kind='proportional')
            left_cell = logo_img
        except Exception:
            left_cell = Paragraph(data.get("StoreName", "My Store"), S_STORE_NAME)
    else:
        left_cell = Paragraph(data.get("StoreName", "My Store"), S_STORE_NAME)

    header = Table(
        [[left_cell, Paragraph("RECEIPT", S_RECEIPT_LBL)]],
        colWidths=[W * 0.6, W * 0.4],
    )
    header.setStyle(TableStyle([
        ("VALIGN",        (0,0),(-1,-1), "TOP"),
        ("BOTTOMPADDING", (0,0),(-1,-1), 0),
    ]))
    story.append(header)
    story.append(Spacer(1, 2*mm))

    for line in [data.get("StoreAddress",""), data.get("StorePhone",""), data.get("StoreEmail","")]:
        if line:
            story.append(Paragraph(line, S_STORE_INFO))
    story.append(Spacer(1, 8*mm))

    # ── Meta: customer left, receipt info right ───────────────────────────
    left_lines = []
    right_data = []

    cust_name  = data.get("CustomerName",  "")
    cust_phone = data.get("CustomerPhone", "")
    cust_email = data.get("CustomerEmail", "")
    cashier    = data.get("Cashier",       "")
    receipt_no = data.get("ReceiptNumber", "")
    sale_date  = (data.get("SaleDate","") or "")[:10]

    if cust_name:
        left_lines += [Paragraph("<b>Bill To</b>", S_BOLD), Paragraph(cust_name, S_NORMAL)]
        if cust_phone: left_lines.append(Paragraph(cust_phone, S_NORMAL))
        if cust_email: left_lines.append(Paragraph(cust_email, S_NORMAL))

    if receipt_no: right_data.append([Paragraph("Receipt #", S_META_LBL), Paragraph(receipt_no, S_META_VAL)])
    if sale_date:  right_data.append([Paragraph("Date",      S_META_LBL), Paragraph(sale_date,  S_META_VAL)])
    if cashier:    right_data.append([Paragraph("Cashier",   S_META_LBL), Paragraph(cashier,    S_META_VAL)])

    if left_lines or right_data:
        right_cell = Paragraph("", S_NORMAL)
        if right_data:
            right_cell = Table(right_data, colWidths=[30*mm, 35*mm])
            right_cell.setStyle(TableStyle([
                ("ALIGN",         (0,0),(-1,-1),"RIGHT"),
                ("VALIGN",        (0,0),(-1,-1),"TOP"),
                ("TOPPADDING",    (0,0),(-1,-1), 1),
                ("BOTTOMPADDING", (0,0),(-1,-1), 1),
                ("LEFTPADDING",   (0,0),(-1,-1), 0),
                ("RIGHTPADDING",  (0,0),(-1,-1), 0),
            ]))
        meta = Table(
            [[left_lines or [Paragraph("",S_NORMAL)], right_cell]],
            colWidths=[W*0.55, W*0.45],
        )
        meta.setStyle(TableStyle([("VALIGN",(0,0),(-1,-1),"TOP")]))
        story.append(meta)
        story.append(Spacer(1, 8*mm))

    # ── Items table ───────────────────────────────────────────────────────
    col_widths = [12*mm, W - 12*mm - 25*mm - 30*mm - 30*mm, 25*mm, 30*mm, 30*mm]

    def th(text, align=TA_LEFT):
        return Paragraph(text, ParagraphStyle("th",fontName="Helvetica-Bold",
                         fontSize=9,textColor=colors.white,alignment=align))
    def td(text, align=TA_LEFT):
        return Paragraph(text, ParagraphStyle("td",fontSize=9,alignment=align,leading=13))

    rows = [[th("QTY",TA_CENTER), th("DESCRIPTION"), th("SIZE",TA_CENTER),
             th("UNIT PRICE",TA_RIGHT), th("AMOUNT",TA_RIGHT)]]

    for line in data.get("Lines", []):
        desc = line.get("Name","")
        attr = line.get("Attribute","")
        if attr: desc += f"\n<font size='8' color='#777777'>{attr}</font>"
        disc = float(line.get("DiscountPct",0))
        if disc > 0: desc += f"\n<font size='8' color='#E53935'>Discount: {disc:.0f}%</font>"
        rows.append([
            td(str(line.get("Qty","")),       TA_CENTER),
            td(desc),
            td(str(line.get("Size","")),       TA_CENTER),
            td(money(line.get("UnitPrice",0)), TA_RIGHT),
            td(money(line.get("LineTotal",0)), TA_RIGHT),
        ])

    tbl = Table(rows, colWidths=col_widths, repeatRows=1)
    tbl.setStyle(TableStyle([
        ("BACKGROUND",    (0,0), (-1,0),  HEADER_BG),
        ("ROWBACKGROUNDS",(0,1), (-1,-1), [colors.white, LIGHT]),
        ("GRID",          (0,0), (-1,-1), 0.5, BORDER),
        ("VALIGN",        (0,0), (-1,-1), "MIDDLE"),
        ("TOPPADDING",    (0,0), (-1,-1), 4),
        ("BOTTOMPADDING", (0,0), (-1,-1), 4),
        ("LEFTPADDING",   (0,0), (-1,-1), 4),
        ("RIGHTPADDING",  (0,0), (-1,-1), 4),
    ]))
    story.append(tbl)
    story.append(Spacer(1, 3*mm))

    # ── Totals ────────────────────────────────────────────────────────────
    right_col = 30*mm
    lbl_col   = W - right_col

    totals = [
        [Paragraph("Subtotal", S_META_LBL), Paragraph(money(data.get("Subtotal",0)), S_META_VAL)],
        [Paragraph("Tax",      S_META_LBL), Paragraph(money(data.get("Tax",0)),      S_META_VAL)],
    ]
    for pay in data.get("Payments", []):
        totals.append([
            Paragraph(pay.get("Label","Payment"), S_META_LBL),
            Paragraph(f"-{money(pay.get('Amount',0))}", S_META_VAL),
        ])

    s_tot = ParagraphStyle("st",fontSize=13,fontName="Helvetica-Bold",alignment=TA_RIGHT)
    totals.append([Paragraph("TOTAL", s_tot), Paragraph(money(data.get("Total",0)), s_tot)])

    due = float(data.get("AmountDue", 0))
    due_col = "#E53935" if due > 0 else "#000000"
    s_due = ParagraphStyle("sd",fontSize=11,fontName="Helvetica-Bold",
                            alignment=TA_RIGHT,textColor=colors.HexColor(due_col))
    totals.append([Paragraph("Amount Due", s_due), Paragraph(money(due), s_due)])

    change = float(data.get("CashChange", 0))
    if change > 0:
        totals.append([Paragraph("Cash Change", S_META_LBL), Paragraph(money(change), S_META_VAL)])

    tot_tbl = Table(totals, colWidths=[lbl_col, right_col])
    tot_tbl.setStyle(TableStyle([
        ("ALIGN",         (0,0),(-1,-1), "RIGHT"),
        ("VALIGN",        (0,0),(-1,-1), "MIDDLE"),
        ("TOPPADDING",    (0,0),(-1,-1), 2),
        ("BOTTOMPADDING", (0,0),(-1,-1), 2),
        ("LINEABOVE",     (0,-3),(-1,-3), 0.75, colors.black),
        ("LINEBELOW",     (0,-3),(-1,-3), 0.75, colors.black),
        ("BACKGROUND",    (0,-3),(-1,-3), LIGHT),
    ]))
    story.append(tot_tbl)
    story.append(Spacer(1, 10*mm))

    # ── Footer ────────────────────────────────────────────────────────────
    story.append(HRFlowable(width="100%", thickness=0.5, color=BORDER))
    story.append(Spacer(1, 3*mm))
    footer_msg = data.get("ReceiptFooter","") or "Thank you for your business!"
    story.append(Paragraph(footer_msg, S_FOOTER))
    if data.get("StoreEmail"):
        story.append(Paragraph(f"Questions? Contact us at {data['StoreEmail']}", S_FOOTER))

    # ── Build ─────────────────────────────────────────────────────────────
    if canvasmaker:
        doc.build(story, canvasmaker=canvasmaker)
    else:
        doc.build(story)

    print(f"PDF saved to {out_path}")


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python generate_receipt.py <json_file> <output_pdf>")
        sys.exit(1)
    with open(sys.argv[1], "r", encoding="utf-8") as f:
        data = json.load(f)
    build(data, sys.argv[2])