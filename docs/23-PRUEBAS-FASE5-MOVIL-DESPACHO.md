# 23 — Pruebas Fase 5: móvil y despacho

## Entorno y reproducción

Validación local 2026-09-06, base F4 `4681fd1f62adb90581ff36d504c54d3dd0f950a6`.
.NET SDK10.0.111, PostgreSQL16-alpine, Keycloak26.7.3, Flutter3.47.2
(d3b14c876900e553bc736ca19295fc09e3853e8e), Dart3.13.2, Java21.
No se migraron bases operativas ni se borraron datos de otros builders.

Desde raíz:

```sh
bash backend/scripts/run-full-integration-tests.sh
bash backend/scripts/run-mobile-e2e.sh
```

Scripts crean/eliminan solo sus contenedores efímeros. PostgreSQL tests recrean
su BD *_test; nunca usar conexión de desarrollo. En mobile: `flutter analyze`,
`flutter test`, `flutter build apk --debug`. [Configuración](../mobile/README.md).

## Evidencia automatizada local

| Gate | Resultado |
|---|---|
| Build backend Release | 0 errores, 0 warnings |
| Backend completo | 216 aprobados, 0 fallidos, 0 omitidos |
| PostgreSQL real | 42 aprobados: 11 F1/F4 + 31 F5 |
| Keycloak real | 9 aprobados, incluido cliente móvil PKCE/callback/roles locales |
| Locales/pipeline HTTP | 165 aprobados; regresión Solicitudes/Inventario/Recepciones/Movimientos |
| Flutter unit/widget | 26 aprobados |
| Flutter analyze | Sin incidencias |
| APK debug | Compilado, mobile/build/app/outputs/flutter-apk/app-debug.apk |
| Flutter → API → PostgreSQL | 1 E2E aprobado, stock50→45, 1 despacho/movimiento/auditoría; rollback íntegro |

F5 cubre parcial, QR malformado/firma/token/otro Ticket, vencido por fecha/estado,
anulado/consumido, cero/negativo/precisión/exceso, tanque/estación
ausente/inactivo/combustible incompatible, inventario ausente/insuficiente,
operador inactivo/rol incorrecto. HTTP cubre401,403 y ownership404.

Mismo Ticket concurrente: un solo despacho/movimiento/descuento. Tickets distintos
sobre stock10 sirviendo7: uno gana, otro INVENTARIO_INSUFICIENTE; ambos saldos3.
xmin rechaza inventario/anulación obsoletos. SQL real comprueba TanqueId NOT NULL,
FK RESTRICT, UNIQUE TicketId y cadena de migraciones completa. Fallo provocado
de auditoría revierte despacho, consumo, inventario y movimiento.

Flutter prueba parsing Ticket/Despacho, UTC/enum/cantidades, errores seguros,
captura única, RBAC visual, formulario/doble toque/éxito/vencido, timeout sin
reintento, refresh concurrente/fallido, logout concurrente y un solo replay401.
E2E usa API/BD reales; identidad y entrada cámara sustituidas explícitamente.
Esto no sustituye login nativo ni hardware.

## Gate humano físico — PENDIENTE TESTER, incluso tras merge PR #9

No hay dispositivo físico conectado. No se declara cámara/login nativo reales
realizados. Adjuntar evidencia sin tokens ni QR reutilizables:

1. Instalar APK configurado; registrar SHA, modelo/Android, fecha y tester.
2. Login Keycloak real, cancelación y retorno por fueltrack://callback.
3. Permitir/denegar cámara; reintentar permiso, fondo/primer plano y rotación.
4. Escanear PDF/QR real; verificar empleado, vehículo, departamento, combustible,
   cantidad, vencimiento/estado. Validar no debe consumir.
5. Confirmar parcial con estación/tanque reales; verificar resultado/stock/BD.
6. Repetir QR en otro dispositivo: consumido. Dos dispositivos simultáneos:
   un solo éxito y descuento.
7. Cortar red durante POST: no repetir; consultar resultado.
8. Sesión expirada/refresh, logout y botón Atrás sin datos privados.

Firma tester: pendiente. F5 ya integrada en main por PR #9; el merge no acredita
la prueba física: `MERGED — MANUAL CAMERA GATE PENDING`.
