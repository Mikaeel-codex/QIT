"""
generate_report.py
Usage: python generate_report.py <csv_path> <pdf_path> <store_name> <report_title> <period>
"""

import sys
import csv
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import mm
from reportlab.platypus import (
    SimpleDocTemplate, Table, TableStyle, Paragraph, Spacer
)
from datetime import datetime

def main():
    if len(sys.argv) < 6:
        print("Usage: generate_report.py <csv> <pdf> <store> <title> <period>")
        sys.exit(1)

    csv_path    = sys.argv[1]
    pdf_path    = sys.argv[2]
    store_name  = sys.argv[3]
    report_title= sys.argv[4]
    period      = sys.argv[5]

    # Read CSV data
    rows = []
    headers = []
    with open(csv_path, newline='', encoding='utf-8') as f:
        reader = csv.reader(f)
        for i, row in enumerate(reader):
            if i == 0:
                headers = row
            else:
                rows.append(row)

    # Build PDF
    doc = SimpleDocTemplate(
        pdf_path,
        pagesize=A4,
        leftMargin=15*mm,
        rightMargin=15*mm,
        topMargin=15*mm,
        bottomMargin=15*mm,
    )

    styles = getSampleStyleSheet()

    story = []

    # Store name
    story.append(Paragraph(
        store_name,
        ParagraphStyle('StoreName', fontSize=16, fontName='Helvetica-Bold', textColor=colors.HexColor('#111111'))
    ))
    story.append(Spacer(1, 4*mm))

    # Report title
    story.append(Paragraph(
        report_title,
        ParagraphStyle('Title', fontSize=13, fontName='Helvetica-Bold', textColor=colors.HexColor('#2F66C8'))
    ))

    # Period
    story.append(Paragraph(
        f"Period: {period}",
        ParagraphStyle('Period', fontSize=9, fontName='Helvetica', textColor=colors.HexColor('#777777'))
    ))

    # Generated timestamp
    story.append(Paragraph(
        f"Generated: {datetime.now().strftime('%d %b %Y  %H:%M')}",
        ParagraphStyle('Generated', fontSize=9, fontName='Helvetica', textColor=colors.HexColor('#777777'))
    ))

    story.append(Spacer(1, 8*mm))

    # Table
    if headers and rows:
        page_width = A4[0] - 30*mm
        col_count  = len(headers)
        col_width  = page_width / col_count

        table_data = [headers] + rows

        table = Table(table_data, colWidths=[col_width] * col_count, repeatRows=1)
        table.setStyle(TableStyle([
            # Header row
            ('BACKGROUND',   (0, 0), (-1, 0),  colors.HexColor('#1A1A2E')),
            ('TEXTCOLOR',    (0, 0), (-1, 0),  colors.white),
            ('FONTNAME',     (0, 0), (-1, 0),  'Helvetica-Bold'),
            ('FONTSIZE',     (0, 0), (-1, 0),  9),
            ('BOTTOMPADDING',(0, 0), (-1, 0),  6),
            ('TOPPADDING',   (0, 0), (-1, 0),  6),

            # Data rows
            ('FONTNAME',     (0, 1), (-1, -1), 'Helvetica'),
            ('FONTSIZE',     (0, 1), (-1, -1), 8),
            ('TOPPADDING',   (0, 1), (-1, -1), 4),
            ('BOTTOMPADDING',(0, 1), (-1, -1), 4),
            ('TEXTCOLOR',    (0, 1), (-1, -1), colors.HexColor('#111111')),

            # Alternating rows
            ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, colors.HexColor('#F5F5F5')]),

            # Grid
            ('GRID',         (0, 0), (-1, -1), 0.4, colors.HexColor('#CCCCCC')),
            ('LINEBELOW',    (0, 0), (-1, 0),  1,   colors.HexColor('#2F66C8')),

            # Alignment
            ('ALIGN',        (0, 0), (-1, -1), 'LEFT'),
            ('VALIGN',       (0, 0), (-1, -1), 'MIDDLE'),
        ]))

        story.append(table)
    else:
        story.append(Paragraph("No data available for this report.", styles['Normal']))

    story.append(Spacer(1, 10*mm))

    # Footer
    story.append(Paragraph(
        f"{store_name}  •  {report_title}  •  {period}",
        ParagraphStyle('Footer', fontSize=7, fontName='Helvetica', textColor=colors.HexColor('#AAAAAA'))
    ))

    doc.build(story)
    print(f"PDF saved: {pdf_path}")

if __name__ == "__main__":
    main()