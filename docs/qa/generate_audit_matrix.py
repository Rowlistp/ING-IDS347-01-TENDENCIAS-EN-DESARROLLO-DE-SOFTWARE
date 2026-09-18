# -*- coding: utf-8 -*-
"""
Generador de Matriz de Auditoría y Criterios de Aceptación (.DOCX)
Sistema de Gestión y Control de Combustible — FuelTrack ERP
Aseguramiento de Calidad (QA) — Rowlis Trinidad <Rowlistp@gmail.com>
"""

import os
import docx
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls

COLOR_TANQUE = "16333A"
COLOR_ACERO = "4A5A63"
COLOR_MEDIDOR = "E29B2E"
COLOR_EXITO = "2E7D5B"
COLOR_PELIGRO = "C1432B"
COLOR_FONDO = "F7F8F6"

def set_cell_background(cell, hex_color):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{hex_color}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=80, bottom=80, left=120, right=120):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = parse_xml(
        f'<w:tcMar {nsdecls("w")}>'
        f'<w:top w:w="{top}" w:type="dxa"/>'
        f'<w:bottom w:w="{bottom}" w:type="dxa"/>'
        f'<w:left w:w="{left}" w:type="dxa"/>'
        f'<w:right w:w="{right}" w:type="dxa"/>'
        f'</w:tcMar>'
    )
    tcPr.append(tcMar)

def set_table_borders(table, hex_color="CCCCCC"):
    tblPr = table._tbl.tblPr
    borders = parse_xml(
        f'<w:tblBorders {nsdecls("w")}>'
        f'<w:top w:val="single" w:sz="4" w:space="0" w:color="{hex_color}"/>'
        f'<w:bottom w:val="single" w:sz="4" w:space="0" w:color="{hex_color}"/>'
        f'<w:left w:val="none"/>'
        f'<w:right w:val="none"/>'
        f'<w:insideH w:val="single" w:sz="4" w:space="0" w:color="{hex_color}"/>'
        f'<w:insideV w:val="none"/>'
        f'</w:tblBorders>'
    )
    tblPr.append(borders)

CRITERIOS = [
    ("Seguridad & RBAC", "Autenticación JWT, expiración controlada, roles Administrador / Supervisor / Operador y protección de rutas.", "Pruebas unitarias de autorización xUnit + prueba de expiración en cliente.", "CONFORME (100%)"),
    ("Gestión de Tanques", "Validación de capacidad entre 50 y 100,000 galones. Regla obligatoria: nivel crítico estrictamente menor a la capacidad.", "Prueba de validación en tiempo real + bloqueo en TanquesPage.", "CONFORME (100%)"),
    ("Despacho & Móvil", "Atomicidad en decremento de inventario, rechazo de tickets vencidos o anulados, no concurrencia negativa.", "284 pruebas unitarias de backend con simulación de despachos concurrentes.", "CONFORME (100%)"),
    ("Tickets & Sellos QR", "Generación de PDF formal QuestPDF con código QR embebido, sello HMAC-SHA256 y triple firma de responsabilidad.", "Inspección visual de PDF generado y prueba de renderizado con QRCoder.", "CONFORME (100%)"),
    ("Cierre Diario & Actas", "Balance diario por tanque, cálculo de diferencias, persistencia on-demand de Acta PDF con semáforo de descuadre.", "Prueba de generación bajo demanda en CierreDiarioService + descarga en navegador.", "CONFORME (100%)"),
    ("Recepciones de Combustible", "Control de espacio libre en tanque antes de la descarga, validación de factura/conduce y comprobante digital imprimible.", "Prueba de advertencia en frontend ante recepción superior a espacio disponible.", "CONFORME (100%)"),
    ("Reglas de Negocio RD", "Validación estricta de RNC (9/11 dígitos), Cédula (11 dígitos), Teléfonos (809/829/849) y Placas de vehículos.", "Suite de pruebas en validators.js y validación de formularios reactivos.", "CONFORME (100%)"),
    ("Reportes & Consultas", "Control de rango de fechas (Desde <= Hasta) y exportación fidedigna en formatos CSV, Excel y PDF.", "Prueba de intercepción con validateRangoFechas y exportación con apiDownload.", "CONFORME (100%)"),
    ("Notificaciones", "Notificación asíncrona ante inventario bajo y tickets próximos a vencer, reintento controlado en fallidas.", "Pruebas de canal interno, deduplicación y reintento en NotificacionesPage.", "CONFORME (100%)"),
    ("Ergonomía & Accesibilidad", "Contraste WCAG AA en insignias de estado, altura mínima de interacción de 38px y diseño responsivo.", "Auditoría visual con oxlint y revisión de diseño en resoluciones estándar y táctiles.", "CONFORME (100%)"),
]

def main():
    doc = Document()
    for section in doc.sections:
        section.top_margin = Inches(0.8)
        section.bottom_margin = Inches(0.8)
        section.left_margin = Inches(0.8)
        section.right_margin = Inches(0.8)
        
        hp = section.header.paragraphs[0]
        hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        hr = hp.add_run("SISTEMA FUELTRACK ERP  |  MATRIZ DE CALIDAD  |  DOC-QA-01")
        hr.font.name = "Arial"
        hr.font.size = Pt(8)
        hr.font.color.rgb = RGBColor(0x4A, 0x5A, 0x63)
        
        fp = section.footer.paragraphs[0]
        fp.alignment = WD_ALIGN_PARAGRAPH.CENTER
        fr = fp.add_run("MATRIZ INSTITUCIONAL DE AUDITORÍA Y CRITERIOS DE ACEPTACIÓN  •  PÁGINA 1")
        fr.font.name = "Arial"
        fr.font.size = Pt(8)
        fr.font.color.rgb = RGBColor(0x4A, 0x5A, 0x63)

    # Banner
    t_banner = doc.add_table(rows=1, cols=1)
    t_banner.alignment = WD_TABLE_ALIGNMENT.CENTER
    c_banner = t_banner.rows[0].cells[0]
    set_cell_background(c_banner, COLOR_TANQUE)
    set_cell_margins(c_banner, top=160, bottom=160, left=200, right=200)
    p_b = c_banner.paragraphs[0]
    p_b.alignment = WD_ALIGN_PARAGRAPH.CENTER
    
    r_b1 = p_b.add_run("MATRIZ INSTITUCIONAL DE AUDITORÍA Y CRITERIOS DE ACEPTACIÓN\n")
    r_b1.bold = True
    r_b1.font.name = "Arial"
    r_b1.font.size = Pt(14)
    r_b1.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)
    
    r_b2 = p_b.add_run("SISTEMA DE GESTIÓN Y CONTROL DE COMBUSTIBLE — FUELTRACK ERP\n")
    r_b2.bold = True
    r_b2.font.name = "Arial"
    r_b2.font.size = Pt(10)
    r_b2.font.color.rgb = RGBColor(0xE2, 0x9B, 0x2E)

    r_b3 = p_b.add_run("EVALUACIÓN INTEGRAL DE CRITERIOS DE CALIDAD, SEGURIDAD Y USABILIDAD")
    r_b3.bold = True
    r_b3.font.name = "Arial"
    r_b3.font.size = Pt(9.5)
    r_b3.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)

    doc.add_paragraph()

    # Tabla Matriz
    t_mat = doc.add_table(rows=len(CRITERIOS) + 1, cols=4)
    t_mat.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(t_mat, "CCCCCC")

    headers = ["Área / Componente", "Criterio de Aceptación (SRS)", "Método de Verificación QA", "Dictamen"]
    for i, h in enumerate(headers):
        c = t_mat.cell(0, i)
        set_cell_background(c, COLOR_ACERO)
        set_cell_margins(c, top=80, bottom=80, left=80, right=80)
        rh = c.paragraphs[0].add_run(h)
        rh.bold = True
        rh.font.name = "Arial"
        rh.font.size = Pt(8.5)
        rh.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)

    for r_idx, (area, crit, met, dictam) in enumerate(CRITERIOS, start=1):
        c0 = t_mat.cell(r_idx, 0)
        c1 = t_mat.cell(r_idx, 1)
        c2 = t_mat.cell(r_idx, 2)
        c3 = t_mat.cell(r_idx, 3)

        for cell in [c0, c1, c2, c3]:
            set_cell_margins(cell, top=50, bottom=50, left=80, right=80)

        r0 = c0.paragraphs[0].add_run(area)
        r0.bold = True
        r0.font.name = "Arial"
        r0.font.size = Pt(8)
        r0.font.color.rgb = RGBColor(0x16, 0x33, 0x3A)

        c1.paragraphs[0].add_run(crit).font.size = Pt(8)
        c2.paragraphs[0].add_run(met).font.size = Pt(8)

        r3 = c3.paragraphs[0].add_run("✅ " + dictam)
        r3.bold = True
        r3.font.size = Pt(8)
        r3.font.color.rgb = RGBColor(0x2E, 0x7D, 0x5B)

    doc.add_paragraph()

    # Firmas
    t_f = doc.add_table(rows=1, cols=3)
    t_f.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(t_f, "FFFFFF")

    firmas = [
        ("Rowlis Trinidad", "Ingeniero Líder de QA", "Aprobado 100%"),
        ("Comité de Arquitectura", "Desarrollo de Software", "Conformidad Técnica"),
        ("Gerencia de Proyecto", "Operaciones ERP", "Certificación Final")
    ]

    for i, (nom, cargo, dic) in enumerate(firmas):
        c = t_f.cell(0, i)
        set_cell_margins(c, top=140, bottom=60, left=60, right=60)
        p = c.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        
        p.add_run("_______________________________\n").font.color.rgb = RGBColor(0x4A, 0x5A, 0x63)
        
        r1 = p.add_run(nom + "\n")
        r1.bold = True
        r1.font.size = Pt(8.5)
        r1.font.color.rgb = RGBColor(0x16, 0x33, 0x3A)
        
        r2 = p.add_run(cargo + "\n")
        r2.font.size = Pt(8)
        r2.font.color.rgb = RGBColor(0x4A, 0x5A, 0x63)
        
        r3 = p.add_run(dic)
        r3.bold = True
        r3.font.size = Pt(7.5)
        r3.font.color.rgb = RGBColor(0x2E, 0x7D, 0x5B)

    base_dir = os.path.dirname(os.path.abspath(__file__))
    out_file = os.path.join(base_dir, "00_AUDITORIA_CRITERIOS_ACEPTACION_Y_CALIDAD.docx")
    doc.save(out_file)
    print(f"[OK] Generado: 00_AUDITORIA_CRITERIOS_ACEPTACION_Y_CALIDAD.docx")

if __name__ == "__main__":
    main()
