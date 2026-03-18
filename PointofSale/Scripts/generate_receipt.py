"""
generate_receipt.py
Called by ReceiptPdfService.cs via Process.Start.

Usage:
    python generate_receipt.py <json_data_file> <output_pdf_path>
"""

import sys
import json
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.units import mm
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, HRFlowable
)
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_LEFT, TA_RIGHT, TA_CENTER

# ── Helpers ───────────────────────────────────────────────────────────────────

def money(val):
    try:
        return f"R{float(val):,.2f}"
    except:
        return str(val)

def style(name, **kwargs):
    base = {
        "fontName":  "Helvetica",
        "fontSize":  10,
        "leading":   14,
        "textColor": colors.black,
    }
    base.update(kwargs)
    return ParagraphStyle(name, **base)

# ── Styles ────────────────────────────────────────────────────────────────────

S_STORE_NAME   = style("storename",  fontSize=18, fontName="Helvetica-Bold")
S_STORE_INFO   = style("storeinfo",  fontSize=9,  textColor=colors.HexColor("#555555"))
S_RECEIPT_LBL  = style("reclbl",     fontSize=22, fontName="Helvetica-Bold", alignment=TA_RIGHT)
S_META_LBL     = style("metalbl",    fontSize=9,  alignment=TA_RIGHT, textColor=colors.HexColor("#555555"))
S_META_VAL     = style("metaval",    fontSize=9,  alignment=TA_RIGHT, fontName="Helvetica-Bold")
S_NORMAL       = style("normal",     fontSize=9)
S_BOLD         = style("bold",       fontSize=9,  fontName="Helvetica-Bold")
S_FOOTER       = style("footer",     fontSize=8,  textColor=colors.HexColor("#777777"))

DARK   = colors.HexColor("#1a1a1a")
LIGHT  = colors.HexColor("#f5f5f5")
BORDER = colors.HexColor("#cccccc")
HEADER_BG = colors.HexColor("#222222")

# ── Main ──────────────────────────────────────────────────────────────────────

def build(data: dict, out_path: str):
    doc = SimpleDocTemplate(
        out_path,
        pagesize=A4,
        leftMargin=18*mm, rightMargin=18*mm,
        topMargin=18*mm,  bottomMargin=18*mm,
    )

    W = A4[0] - 36*mm   # usable width
    story = []

    # ── Header: Store name left, RECEIPT right ────────────────────────────
    header = Table(
        [[
            Paragraph(data.get("StoreName", "My Store"), S_STORE_NAME),
            Paragraph("RECEIPT", S_RECEIPT_LBL),
        ]],
        colWidths=[W * 0.6, W * 0.4],
    )
    header.setStyle(TableStyle([
        ("VALIGN",      (0,0), (-1,-1), "TOP"),
        ("BOTTOMPADDING",(0,0),(-1,-1), 0),
    ]))
    story.append(header)
    story.append(Spacer(1, 2*mm))

    # Store address block
    addr = data.get("StoreAddress", "")
    phone = data.get("StorePhone", "")
    email = data.get("StoreEmail", "")
    for line in [addr, phone, email]:
        if line:
            story.append(Paragraph(line, S_STORE_INFO))
    story.append(Spacer(1, 8*mm))

    # ── Meta row: customer left, receipt # / date right ───────────────────
    cust_name  = data.get("CustomerName", "")
    cust_phone = data.get("CustomerPhone", "")
    cust_email = data.get("CustomerEmail", "")
    cashier    = data.get("Cashier", "")

    left_lines  = []
    right_data  = []

    if cust_name:
        left_lines.append(Paragraph("<b>Bill To</b>", S_BOLD))
        left_lines.append(Paragraph(cust_name, S_NORMAL))
        if cust_phone: left_lines.append(Paragraph(cust_phone, S_NORMAL))
        if cust_email: left_lines.append(Paragraph(cust_email, S_NORMAL))

    receipt_no = data.get("ReceiptNumber", "")
    sale_date  = data.get("SaleDate", "")[:10] if data.get("SaleDate") else ""

    if receipt_no:
        right_data += [
            [Paragraph("Receipt #", S_META_LBL), Paragraph(receipt_no, S_META_VAL)],
        ]
    if sale_date:
        right_data += [
            [Paragraph("Date", S_META_LBL), Paragraph(sale_date, S_META_VAL)],
        ]
    if cashier:
        right_data += [
            [Paragraph("Cashier", S_META_LBL), Paragraph(cashier, S_META_VAL)],
        ]

    if left_lines or right_data:
        right_tbl = Table(right_data, colWidths=[30*mm, 35*mm]) if right_data else Paragraph("", S_NORMAL)
        if right_data:
            right_tbl.setStyle(TableStyle([
                ("ALIGN",   (0,0),(-1,-1), "RIGHT"),
                ("VALIGN",  (0,0),(-1,-1), "TOP"),
                ("TOPPADDING",   (0,0),(-1,-1), 1),
                ("BOTTOMPADDING",(0,0),(-1,-1), 1),
                ("LEFTPADDING",  (0,0),(-1,-1), 0),
                ("RIGHTPADDING", (0,0),(-1,-1), 0),
            ]))

        left_cell = left_lines if left_lines else [Paragraph("", S_NORMAL)]
        meta = Table(
            [[left_cell, right_tbl]],
            colWidths=[W * 0.55, W * 0.45],
        )
        meta.setStyle(TableStyle([
            ("VALIGN", (0,0),(-1,-1), "TOP"),
        ]))
        story.append(meta)
        story.append(Spacer(1, 8*mm))

    # ── Items table ───────────────────────────────────────────────────────
    col_widths = [12*mm, W - 12*mm - 25*mm - 30*mm - 30*mm, 25*mm, 30*mm, 30*mm]

    tbl_data = [[
        Paragraph("QTY",        ParagraphStyle("th", fontName="Helvetica-Bold", fontSize=9, textColor=colors.white, alignment=TA_CENTER)),
        Paragraph("DESCRIPTION",ParagraphStyle("th", fontName="Helvetica-Bold", fontSize=9, textColor=colors.white, alignment=TA_LEFT)),
        Paragraph("SIZE",       ParagraphStyle("th", fontName="Helvetica-Bold", fontSize=9, textColor=colors.white, alignment=TA_CENTER)),
        Paragraph("UNIT PRICE", ParagraphStyle("th", fontName="Helvetica-Bold", fontSize=9, textColor=colors.white, alignment=TA_RIGHT)),
        Paragraph("AMOUNT",     ParagraphStyle("th", fontName="Helvetica-Bold", fontSize=9, textColor=colors.white, alignment=TA_RIGHT)),
    ]]

    lines = data.get("Lines", [])
    for i, line in enumerate(lines):
        name = line.get("Name", "")
        attr = line.get("Attribute", "")
        desc = f"{name}"
        if attr: desc += f"\n<font size='8' color='#777777'>{attr}</font>"

        disc = float(line.get("DiscountPct", 0))
        if disc > 0:
            desc += f"\n<font size='8' color='#E53935'>Discount: {disc:.0f}%</font>"

        tbl_data.append([
            Paragraph(str(line.get("Qty", "")),              ParagraphStyle("td_c", fontSize=9, alignment=TA_CENTER)),
            Paragraph(desc,                                   ParagraphStyle("td_l", fontSize=9, alignment=TA_LEFT, leading=13)),
            Paragraph(str(line.get("Size", "")),             ParagraphStyle("td_c", fontSize=9, alignment=TA_CENTER)),
            Paragraph(money(line.get("UnitPrice", 0)),       ParagraphStyle("td_r", fontSize=9, alignment=TA_RIGHT)),
            Paragraph(money(line.get("LineTotal", 0)),       ParagraphStyle("td_r", fontSize=9, alignment=TA_RIGHT)),
        ])

    tbl = Table(tbl_data, colWidths=col_widths, repeatRows=1)
    tbl_style = [
        # Header row
        ("BACKGROUND",    (0, 0), (-1,  0), HEADER_BG),
        ("ROWBACKGROUNDS",(0, 1), (-1, -1), [colors.white, LIGHT]),
        ("GRID",          (0, 0), (-1, -1), 0.5, BORDER),
        ("VALIGN",        (0, 0), (-1, -1), "MIDDLE"),
        ("TOPPADDING",    (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("LEFTPADDING",   (0, 0), (-1, -1), 4),
        ("RIGHTPADDING",  (0, 0), (-1, -1), 4),
    ]
    tbl.setStyle(TableStyle(tbl_style))
    story.append(tbl)
    story.append(Spacer(1, 3*mm))

    # ── Totals block ──────────────────────────────────────────────────────
    right_col = 30*mm
    lbl_col   = W - right_col

    totals = []
    totals.append([Paragraph("Subtotal", S_META_LBL), Paragraph(money(data.get("Subtotal", 0)), S_META_VAL)])
    totals.append([Paragraph("Tax",      S_META_LBL), Paragraph(money(data.get("Tax", 0)),      S_META_VAL)])

    # Payment splits shown as deductions
    for pay in data.get("Payments", []):
        totals.append([
            Paragraph(pay.get("Label", "Payment"), S_META_LBL),
            Paragraph(f"-{money(pay.get('Amount', 0))}", S_META_VAL),
        ])

    # TOTAL row
    total_lbl = ParagraphStyle("totlbl", fontSize=13, fontName="Helvetica-Bold", alignment=TA_RIGHT)
    total_val = ParagraphStyle("totval", fontSize=13, fontName="Helvetica-Bold", alignment=TA_RIGHT)
    totals.append([Paragraph("TOTAL", total_lbl), Paragraph(money(data.get("Total", 0)), total_val)])

    # Amount Due
    due = float(data.get("AmountDue", 0))
    if due > 0:
        due_lbl = ParagraphStyle("duelbl", fontSize=11, fontName="Helvetica-Bold", alignment=TA_RIGHT, textColor=colors.HexColor("#E53935"))
        due_val = ParagraphStyle("dueval", fontSize=11, fontName="Helvetica-Bold", alignment=TA_RIGHT, textColor=colors.HexColor("#E53935"))
        totals.append([Paragraph("Amount Due", due_lbl), Paragraph(money(due), due_val)])
    else:
        totals.append([Paragraph("Amount Due", ParagraphStyle("paidlbl", fontSize=11, fontName="Helvetica-Bold", alignment=TA_RIGHT)),
                       Paragraph(money(0), ParagraphStyle("paidval", fontSize=11, fontName="Helvetica-Bold", alignment=TA_RIGHT))])

    change = float(data.get("CashChange", 0))
    if change > 0:
        totals.append([Paragraph("Cash Change", S_META_LBL), Paragraph(money(change), S_META_VAL)])

    totals_tbl = Table(totals, colWidths=[lbl_col, right_col])
    totals_tbl.setStyle(TableStyle([
        ("ALIGN",          (0,0),(-1,-1), "RIGHT"),
        ("VALIGN",         (0,0),(-1,-1), "MIDDLE"),
        ("TOPPADDING",     (0,0),(-1,-1), 2),
        ("BOTTOMPADDING",  (0,0),(-1,-1), 2),
        ("LINEABOVE",      (0,-3),(-1,-3), 0.75, colors.black),   # line above TOTAL
        ("LINEBELOW",      (0,-3),(-1,-3), 0.75, colors.black),   # line below TOTAL
        ("BACKGROUND",     (0,-3),(-1,-3), LIGHT),
    ]))
    story.append(totals_tbl)
    story.append(Spacer(1, 10*mm))

    # ── Footer ────────────────────────────────────────────────────────────
    story.append(HRFlowable(width="100%", thickness=0.5, color=BORDER))
    story.append(Spacer(1, 3*mm))
    story.append(Paragraph("Thank you for your business!", S_FOOTER))
    if data.get("StoreEmail"):
        story.append(Paragraph(f"Questions? Contact us at {data['StoreEmail']}", S_FOOTER))

    doc.build(story)
    print(f"PDF saved to {out_path}")


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python generate_receipt.py <json_file> <output_pdf>")
        sys.exit(1)

    with open(sys.argv[1], "r", encoding="utf-8") as f:
        data = json.load(f)

    build(data, sys.argv[2])