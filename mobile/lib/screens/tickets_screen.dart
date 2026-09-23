import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import '../app/providers.dart';
import '../core/errors.dart';
import '../core/models.dart';
import '../theme/app_theme.dart';
import '../widgets/ticket_card.dart';

class TicketsScreen extends ConsumerStatefulWidget {
  const TicketsScreen({super.key});

  @override
  ConsumerState<TicketsScreen> createState() => _TicketsScreenState();
}

class _TicketsScreenState extends ConsumerState<TicketsScreen> {
  Future<List<Ticket>>? _future;
  String _searchQuery = '';
  String _selectedFilter = 'Todos';

  final List<String> _filters = [
    'Todos',
    'Creado',
    'Consumido',
    'Próximo a vencer',
    'Vencido',
  ];

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  void _refresh() {
    setState(() {
      // BUG FIX: capturamos errores localmente; nunca propagamos al sessionProvider.
      // Cualquier ApiFailure (incluída SESSION_EXPIRED) queda contenida en el
      // FutureBuilder y se muestra como error local con opción de reintentar.
      _future = ref.read(apiProvider).tickets().catchError((Object e) {
        throw ApiError(friendlyError(e));
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      // Sin AppBar propio — el HomeScreen ya muestra el AppBar con el título de pestaña
      body: Column(
        children: [
          // ── Filtros ──────────────────────────────────────────────────────
          Container(
            padding: const EdgeInsets.fromLTRB(14, 10, 14, 10),
            decoration: const BoxDecoration(
              color: Colors.white,
              border: Border(bottom: BorderSide(color: AppColors.cardBorder)),
            ),
            child: Column(
              children: [
                // Búsqueda
                SizedBox(
                  height: 38,
                  child: TextField(
                    onChanged: (val) =>
                        setState(() => _searchQuery = val.trim().toLowerCase()),
                    style: GoogleFonts.publicSans(
                      fontSize: 13,
                      color: AppColors.textPrimary,
                    ),
                    decoration: InputDecoration(
                      hintText: 'Buscar por código, conductor o placa…',
                      hintStyle: GoogleFonts.publicSans(
                        fontSize: 13,
                        color: AppColors.textMuted,
                      ),
                      prefixIcon: const Icon(
                        Icons.search_rounded,
                        size: 18,
                        color: AppColors.textSecondary,
                      ),
                      suffixIcon: _searchQuery.isNotEmpty
                          ? IconButton(
                              icon: const Icon(Icons.clear_rounded, size: 16),
                              onPressed: () =>
                                  setState(() => _searchQuery = ''),
                            )
                          : null,
                      contentPadding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 0,
                      ),
                      filled: true,
                      fillColor: AppColors.background,
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(4),
                        borderSide: const BorderSide(
                          color: AppColors.cardBorder,
                        ),
                      ),
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(4),
                        borderSide: const BorderSide(
                          color: AppColors.cardBorder,
                        ),
                      ),
                      focusedBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(4),
                        borderSide: const BorderSide(
                          color: AppColors.primary,
                          width: 1.5,
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 10),

                // Chips de filtro
                SizedBox(
                  height: 30,
                  child: ListView.separated(
                    scrollDirection: Axis.horizontal,
                    itemCount: _filters.length,
                    separatorBuilder: (_, _) => const SizedBox(width: 6),

                    itemBuilder: (_, i) {
                      final filter = _filters[i];
                      final selected = _selectedFilter == filter;
                      return GestureDetector(
                        onTap: () => setState(() => _selectedFilter = filter),
                        child: AnimatedContainer(
                          duration: const Duration(milliseconds: 150),
                          padding: const EdgeInsets.symmetric(
                            horizontal: 12,
                            vertical: 5,
                          ),
                          decoration: BoxDecoration(
                            color: selected ? AppColors.primary : Colors.white,
                            borderRadius: BorderRadius.circular(4),
                            border: Border.all(
                              color: selected
                                  ? AppColors.primary
                                  : AppColors.cardBorder,
                            ),
                          ),
                          child: Text(
                            filter,
                            style: GoogleFonts.publicSans(
                              fontSize: 12,
                              fontWeight: selected
                                  ? FontWeight.w600
                                  : FontWeight.w500,
                              color: selected
                                  ? Colors.white
                                  : AppColors.textSecondary,
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                ),
              ],
            ),
          ),

          // ── Lista de tickets ─────────────────────────────────────────────
          Expanded(
            child: FutureBuilder<List<Ticket>>(
              future: _future,
              builder: (context, snapshot) {
                if (snapshot.connectionState != ConnectionState.done) {
                  return const Center(
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: AppColors.primary,
                    ),
                  );
                }

                if (snapshot.hasError) {
                  return Center(
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Container(
                            padding: const EdgeInsets.all(14),
                            decoration: BoxDecoration(
                              color: AppColors.background,
                              borderRadius: BorderRadius.circular(4),
                              border: Border.all(color: AppColors.cardBorder),
                            ),
                            child: const Icon(
                              Icons.cloud_off_rounded,
                              size: 36,
                              color: AppColors.textMuted,
                            ),
                          ),
                          const SizedBox(height: 14),
                          Text(
                            'No se pudieron cargar los tickets',
                            style: GoogleFonts.publicSans(
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 6),
                          Text(
                            '${snapshot.error}',
                            textAlign: TextAlign.center,
                            style: GoogleFonts.publicSans(
                              fontSize: 12,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          const SizedBox(height: 18),
                          SizedBox(
                            height: 40,
                            child: OutlinedButton.icon(
                              onPressed: _refresh,
                              style: OutlinedButton.styleFrom(
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(4),
                                ),
                              ),
                              icon: const Icon(Icons.refresh_rounded, size: 16),
                              label: Text(
                                'Reintentar',
                                style: GoogleFonts.publicSans(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }

                final tickets = snapshot.data ?? [];
                final filtered = tickets.where((t) {
                  final matchesSearch =
                      _searchQuery.isEmpty ||
                      t.code.toLowerCase().contains(_searchQuery) ||
                      t.employee.toLowerCase().contains(_searchQuery) ||
                      t.vehicle.toLowerCase().contains(_searchQuery);

                  final matchesFilter =
                      _selectedFilter == 'Todos' ||
                      t.state == _selectedFilter ||
                      t.stateLabel == _selectedFilter;

                  return matchesSearch && matchesFilter;
                }).toList();

                if (filtered.isEmpty) {
                  return Center(
                    child: Padding(
                      padding: const EdgeInsets.all(32),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Container(
                            padding: const EdgeInsets.all(14),
                            decoration: BoxDecoration(
                              color: AppColors.primaryLight,
                              borderRadius: BorderRadius.circular(4),
                              border: Border.all(color: AppColors.cardBorder),
                            ),
                            child: const Icon(
                              Icons.receipt_long_rounded,
                              size: 36,
                              color: AppColors.primary,
                            ),
                          ),
                          const SizedBox(height: 14),
                          Text(
                            'No se encontraron tickets',
                            style: GoogleFonts.publicSans(
                              fontSize: 15,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 6),
                          Text(
                            _searchQuery.isNotEmpty ||
                                    _selectedFilter != 'Todos'
                                ? 'Ajusta los filtros de búsqueda.'
                                : 'No hay tickets disponibles para tu usuario.',
                            textAlign: TextAlign.center,
                            style: GoogleFonts.publicSans(
                              fontSize: 13,
                              color: AppColors.textSecondary,
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }

                return RefreshIndicator(
                  onRefresh: () async => _refresh(),
                  color: AppColors.primary,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(14),
                    itemCount: filtered.length,
                    itemBuilder: (context, index) =>
                        TicketCard(ticket: filtered[index]),
                  ),
                );
              },
            ),
          ),
        ],
      ),

      // Botón flotante de actualizar
      floatingActionButton: FloatingActionButton.small(
        onPressed: _refresh,
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
        elevation: 2,
        tooltip: 'Actualizar tickets',
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(6)),
        child: const Icon(Icons.refresh_rounded, size: 20),
      ),
    );
  }
}

/// Error wrapper local para no contaminar la sesión
class ApiError implements Exception {
  const ApiError(this.message);
  final String message;
  @override
  String toString() => message;
}
