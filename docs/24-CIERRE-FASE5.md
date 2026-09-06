# 24 — Cierre Fase 5

## Alcance entregado

RF-12: API de despacho sobre entidad existente. RF-13: Flutter Android con login
institucional, scanner, validación visual, confirmación, resultado y consulta.
Rama feature/builder2-fase5-mobile-despacho desde F4 corregida4681fd1;
sin merge a main, PR abierto ni reescritura de F4.

## Arquitectura, transacción e inventario

DespachosController resuelve identidad/permisos y delega a DispatchService.
TicketService/TicketQrService F4 comprueban ECDSA P-256/hash/token y concordancia
con BD dentro del bloqueo. No se confía en el escaneo previo. Se bloquea Ticket,
luego Inventario; Tanque/Estación tienen lectura protegida y la caducidad se
revalida tras esperar locks.

Un commit incluye Despacho, Ticket Consumido, ExistenciaActual/Disponibilidad,
Movimiento Salida negativo y Auditoría DESPACHO_REGISTRADO. UNIQUE TicketId
impide duplicación incluso fuera del servicio. Parcial consume todo el ticket.
xmin impide sobrescrituras obsoletas F6 o anulaciones F4; middleware409 evita
reformular esos controladores. Cualquier error revierte todos los cambios.

TanqueId obligatorio RESTRICT y Estación independiente. Inventario es la fuente
del saldo; NivelActual no se toca. Se conservan instantáneas de ambos saldos.
Migración20260906195749_AddPhase5DispatchIntegration probada con cadena completa.
Si hay despachos históricos, aborta antes de cambiar datos: requiere mapeo de
tanques explícito revisado, pues Estación no permite inferirlo de forma segura.

## Seguridad móvil y API

POST solo Despachador local activo; actor del token, no JSON. Lectores
operacionales globales, Despachador solo propio, Solicitante sin despacho.
Estaciones añade lectura activa; tanques reutiliza API existente. Auth/me da
roles locales para la UI, nunca permisos derivados de roles externos.

Dio, Riverpod, AppAuth PKCE S256, Secure Storage y mobile_scanner. Tokens fuera
de logs/Git; refresh single-flight/un replay401/logout tras segundo401 o fallo.
HTTPS por defecto y dart-define para configuración. Captura pausa/deduplica QR;
confirmación exige identidad/vehículo. Respuesta incierta bloquea otro POST
hasta consultar y revalidar. No hay cola offline.

## Evidencia y CI

[23 — Pruebas](23-PRUEBAS-FASE5-MOVIL-DESPACHO.md): 216 backend +26 Flutter y1
E2E real aprobados, analyze limpio, APK debug compilado. Workflows Backend
Security/Mobile Phase5 ejecutan gates para la rama. Enlaces/SHA definitivos en
informe de entrega tras push. APK sin defines acredita build, no conexión productiva.

## Protección del trabajo compartido

No se modifican SolicitudesController, InventarioController, RecepcionesController,
MovimientosController, TanquesController, ProveedoresController ni frontend.
Compartidos ajustados: Despacho.cs, AppDbContext, snapshot, migración, Program.cs
(DI/conflicto409), AuthController(me), workflow/documentación. Sin eliminar
trabajo ajeno ni migraciones anteriores.

## Pendientes deliberados

Cámara/login nativo/QR en dispositivo: **PENDIENTE TESTER antes de merge**.
Firma Android productiva/TLS/operación IdP requieren despliegue. SMTP/SMS:F9;
cierre diario:Builder1/F7; reportes:F8. No se simulan como hechos.
Estado sujeto a CI verde: `AUTOMATED READY — MANUAL CAMERA GATE PENDING`.
