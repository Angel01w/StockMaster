<template>
	<div class="page">
		<div class="content">
			<div class="hdr">
				<div class="hdr-left">
					<div class="cube">
						<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
							<path d="M12 2 3 7l9 5 9-5-9-5Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
							<path d="M3 7v10l9 5 9-5V7" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
							<path d="M12 12v10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
						</svg>
					</div>
					<div class="h1">Control de Inventario</div>
				</div>

				<button v-if="canEdit" class="btn-primary" type="button" @click="openCreate">
					<span class="plus">＋</span>
					Registrar Movimiento
				</button>
			</div>

			<div v-if="loadError" class="alert">
				{{ loadError }}
			</div>

			<div class="stats">
				<div class="stat">
					<div class="stat-ic ic-blue">
						<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
							<path d="M4 7h16" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
							<path d="M7 7V5.5A2.5 2.5 0 0 1 9.5 3h5A2.5 2.5 0 0 1 17 5.5V7" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
							<path d="M6 7v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V7" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
						</svg>
					</div>
					<div>
						<div class="stat-num">{{ totalMovimientos }}</div>
						<div class="stat-lbl">Total Movimientos</div>
					</div>
				</div>

				<div class="stat">
					<div class="stat-ic ic-green">
						<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
							<path d="M12 19V5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
							<path d="M7 10l5-5 5 5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
							<circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.2" opacity=".25" />
						</svg>
					</div>
					<div>
						<div class="stat-num green">{{ totalEntradas }}</div>
						<div class="stat-lbl">Total Entradas</div>
					</div>
				</div>

				<div class="stat">
					<div class="stat-ic ic-red">
						<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
							<path d="M12 5v14" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
							<path d="M7 14l5 5 5-5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
							<circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.2" opacity=".25" />
						</svg>
					</div>
					<div>
						<div class="stat-num red">{{ totalSalidas }}</div>
						<div class="stat-lbl">Total Salidas</div>
					</div>
				</div>
			</div>

			<div class="card">
				<div class="toolbar">
					<div class="search">
						<div class="search-ic">
							<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
								<path d="M21 21l-4.3-4.3" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
								<circle cx="11" cy="11" r="7" stroke="currentColor" stroke-width="1.8" />
							</svg>
						</div>
						<input v-model="search" class="search-in" placeholder="Buscar movimientos..." />
					</div>

					<div class="tabs">
						<button class="tab" :class="{ active: filterTipo === 'todos' }" @click="filterTipo='todos'">Todos</button>
						<button class="tab" :class="{ active: filterTipo === 'entrada' }" @click="filterTipo='entrada'">Entradas</button>
						<button class="tab" :class="{ active: filterTipo === 'salida' }" @click="filterTipo='salida'">Salidas</button>
					</div>
				</div>

				<div v-if="loading" class="mutedLine">Cargando inventario...</div>
				<div v-else-if="filteredRows.length === 0" class="mutedLine">No hay movimientos para mostrar.</div>

				<div v-else class="table">
					<div class="thead">
						<div>Fecha</div>
						<div>Tipo</div>
						<div>Producto</div>
						<div class="num">Cantidad</div>
						<div>Motivo</div>
						<div>Documento</div>
						<div>Usuario</div>
					</div>

					<div class="trow" v-for="m in filteredRows" :key="rowKey(m)">
						<div class="date">{{ formatDate(m.fecha) }}</div>

						<div>
							<span class="pill" :class="m.tipo === 'Entrada' ? 'pill-green' : 'pill-red'">
								<span class="dot"></span>
								{{ m.tipo }}
							</span>
						</div>

						<div class="prod">{{ m.productoNombre }}</div>
						<div class="num qty">{{ m.cantidad }}</div>
						<div class="muted">{{ motivoLabel(m) }}</div>

						<div class="doc">
							<span v-if="m.documento" class="doc-ic">
								<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
									<path d="M7 3h7l3 3v15a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
									<path d="M14 3v4h4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
								</svg>
							</span>
							<span class="doc-txt">{{ m.documento || "-" }}</span>
						</div>

						<div class="muted">{{ usuarioLabel(m) }}</div>
					</div>

					<div class="tfoot">
						<div class="foot-left">Mostrando {{ filteredRows.length }} de {{ rows.length }} movimientos</div>
						<div class="foot-right">Mostrando {{ filteredRows.length }} de {{ rows.length }} movimientos</div>
					</div>
				</div>
			</div>

			<div v-if="isOpen && canEdit" class="modalOverlay" @click.self="closeModal">
				<div class="modal" role="dialog" aria-modal="true">
					<div class="modalHead">
						<div class="modalTitle">Registrar Movimiento</div>
						<button class="xBtn" type="button" @click="closeModal" aria-label="Cerrar">×</button>
					</div>

					<div class="modalBody">
						<div v-if="apiError" class="alert">{{ apiError }}</div>

						<div class="grid2">
							<div class="field">
								<label>Fecha</label>
								<input type="date" v-model="form.fecha" :disabled="!canEdit" />
							</div>

							<div class="field">
								<label>Tipo de Movimiento</label>
								<select v-model="form.tipo" :disabled="tipoBloqueado || !canEdit">
									<option value="Entrada">Entrada</option>
									<option value="Salida">Salida</option>
								</select>
								<div v-if="tipoBloqueado" class="miniHint">
									El tipo se ajusta automáticamente según el motivo seleccionado.
								</div>
							</div>
						</div>

						<div class="field">
							<label>Producto</label>
							<select v-model.number="form.idProducto" :disabled="productosLoading || !canEdit">
								<option :value="null" disabled>
									{{ productosLoading ? "Cargando productos..." : "Seleccione un producto" }}
								</option>
								<option v-for="p in productos" :key="String(p.idProducto)" :value="Number(p.idProducto)">
									{{ p.nombre }}
								</option>
							</select>

							<div v-if="!productosLoading && productosLoadedOnce && productos.length === 0" class="miniWarn">
								No hay productos registrados (api/Productos devolvió vacío).
							</div>
							<div v-else-if="productosError" class="miniWarn">{{ productosError }}</div>
						</div>

						<div class="grid2">
							<div class="field">
								<label>Cantidad</label>
								<input type="number" min="0" step="1" v-model.number="form.cantidad" :disabled="!canEdit" />
							</div>

							<div class="field">
								<label>Documento (Opcional)</label>
								<input v-model.trim="form.documento" placeholder="Ej: FAC-2024-001" autocomplete="off" :disabled="!canEdit" />
							</div>
						</div>

						<div class="field">
							<label>Motivo</label>
							<select v-model.number="form.idMotivo" :disabled="motivosLoading || !canEdit">
								<option :value="null" disabled>
									{{ motivosLoading ? "Cargando motivos..." : "Seleccione un motivo" }}
								</option>
								<option v-for="mm in motivos" :key="String(mm.idMotivo)" :value="Number(mm.idMotivo)">
									{{ mm.nombre }}
								</option>
							</select>

							<div v-if="!motivosLoading && motivosLoadedOnce && motivos.length === 0" class="miniWarn">
								No hay motivos registrados (api/Motivos devolvió vacío).
							</div>
							<div v-else-if="motivosError" class="miniWarn">{{ motivosError }}</div>
						</div>

						<div class="field">
							<label>Usuario</label>
							<select v-model.number="form.idUsuario" :disabled="usuariosLoading || !canEdit">
								<option :value="null" disabled>
									{{ usuariosLoading ? "Cargando usuarios..." : "Seleccione un usuario" }}
								</option>
								<option v-for="u in usuarios" :key="String(u.idUsuario)" :value="Number(u.idUsuario)">
									{{ u.nombreCompleto }}
								</option>
							</select>

							<div v-if="!usuariosLoading && usuariosLoadedOnce && usuarios.length === 0" class="miniWarn">
								No hay usuarios registrados (api/Usuarios devolvió vacío).
							</div>
							<div v-else-if="usuariosError" class="miniWarn">{{ usuariosError }}</div>
						</div>
					</div>

					<div class="modalFoot">
						<button class="btnLink" type="button" @click="closeModal">Cancelar</button>

						<button class="btnPrimary" type="button" :disabled="saving || motivosLoading || productosLoading || usuariosLoading || !canEdit" @click="createMovement">
							{{ saving ? "Registrando..." : "Registrar Movimiento" }}
						</button>
					</div>
				</div>
			</div>

		</div>
	</div>
</template>

<script setup>
	import { computed, onMounted, reactive, ref, watch } from "vue";
	import { getUser } from "../router/auth.service";
	import { getPermsSafe } from "../services/permissions";
	import { apiFetch } from "../services/api";

	const user = computed(() => getUser());
	const perms = computed(() => getPermsSafe(user.value));
	const canEdit = computed(() => perms.value?.canEditInventario === true || perms.value?.canEditAll === true);

	const MOV_ENDPOINT = "/api/Movimientos";
	const PRODUCTOS_ENDPOINT = "/api/Productos";

	const USUARIOS_ENDPOINTS = [
		"/api/Usuarios",
		"/api/Usuario",
		"/api/Users",
		"/api/UsuariosSistema",
	];

	const MOTIVOS_ENDPOINTS = [
		"/api/Motivos",
		"/api/Motivo",
		"/api/MotivosMovimiento",
		"/api/MotivoMovimiento",
	];

	const search = ref("");
	const filterTipo = ref("todos");

	const loading = ref(false);
	const loadError = ref("");

	const isOpen = ref(false);
	const saving = ref(false);
	const apiError = ref("");

	const rows = ref([]);

	const productos = ref([]);
	const productosError = ref("");
	const productosLoading = ref(false);
	const productosLoadedOnce = ref(false);

	const motivos = ref([]);
	const motivosError = ref("");
	const motivosLoading = ref(false);
	const motivosLoadedOnce = ref(false);

	const usuarios = ref([]);
	const usuariosError = ref("");
	const usuariosLoading = ref(false);
	const usuariosLoadedOnce = ref(false);

	const emptyForm = () => ({
		fecha: toDateInputValue(new Date()),
		tipo: "Entrada",
		idProducto: null,
		cantidad: 0,
		idMotivo: null,
		documento: "",
		idUsuario: null,
	});
	const form = reactive(emptyForm());

	const motivoSeleccionado = computed(() => {
		const id = Number(form.idMotivo);
		if (!id) return null;
		return motivos.value.find((m) => Number(m.idMotivo) === id) ?? null;
	});

	const tipoAplicaNorm = computed(() => {
		const v = motivoSeleccionado.value?.tipoAplica;
		return String(v ?? "").trim().toLowerCase();
	});

	const tipoBloqueado = computed(() => {
		return tipoAplicaNorm.value === "entrada" || tipoAplicaNorm.value === "salida";
	});

	function syncTipoConMotivo() {
		const t = tipoAplicaNorm.value;
		if (t === "entrada") form.tipo = "Entrada";
		else if (t === "salida") form.tipo = "Salida";
	}

	watch(
		() => form.idMotivo,
		() => {
			syncTipoConMotivo();
		}
	);

	onMounted(async () => {
		await loadAll();
	});

	async function loadAll() {
		loading.value = true;
		loadError.value = "";
		try {
			await Promise.all([loadProductos(), loadMotivos(), loadUsuarios()]);
			await loadMovimientos();
			syncTipoConMotivo();
		} catch (e) {
			loadError.value = e?.message ?? "Error cargando inventario.";
		} finally {
			loading.value = false;
		}
	}

	function normalizeList(data) {
		if (Array.isArray(data)) return data;
		if (Array.isArray(data?.$values)) return data.$values;
		if (Array.isArray(data?.items)) return data.items;
		if (Array.isArray(data?.data)) return data.data;
		if (Array.isArray(data?.result)) return data.result;
		if (Array.isArray(data?.value)) return data.value;
		if (Array.isArray(data?.results)) return data.results;
		return [];
	}

	function normalizeProducto(p) {
		const idProducto = p?.idProducto ?? p?.IdProducto ?? p?.id ?? p?.Id ?? null;
		const nombre = p?.nombre ?? p?.Nombre ?? p?.name ?? p?.descripcion ?? p?.Descripcion ?? "";
		return {
			...p,
			idProducto: idProducto != null ? Number(idProducto) : null,
			nombre: String(nombre ?? "").trim(),
		};
	}

	function normalizeMotivo(m) {
		const idMotivo = m?.idMotivo ?? m?.IdMotivo ?? m?.id ?? m?.Id ?? m?.idMotivoMovimiento ?? m?.IdMotivoMovimiento ?? null;
		const nombre = m?.nombre ?? m?.Nombre ?? m?.descripcion ?? m?.Descripcion ?? "";
		const tipoAplica = m?.tipoAplica ?? m?.TipoAplica ?? null;
		return {
			...m,
			idMotivo: idMotivo != null ? Number(idMotivo) : null,
			nombre: String(nombre ?? "").trim(),
			tipoAplica,
		};
	}

	function normalizeUsuario(u) {
		const idUsuario = u?.idUsuario ?? u?.IdUsuario ?? u?.id ?? u?.Id ?? null;
		const nombreCompleto =
			u?.nombreCompleto ??
			u?.NombreCompleto ??
			u?.nombre ??
			u?.Nombre ??
			u?.username ??
			u?.Username ??
			u?.email ??
			u?.Email ??
			"";
		return {
			...u,
			idUsuario: idUsuario != null ? Number(idUsuario) : null,
			nombreCompleto: String(nombreCompleto ?? "").trim(),
		};
	}

	function normalizeMovement(m) {
		const fecha = m?.fecha ?? m?.Fecha ?? m?.createdAt ?? m?.CreatedAt ?? m?.date ?? null;

		let tipo = m?.tipo ?? m?.Tipo ?? "Entrada";
		if (typeof tipo === "number") tipo = tipo === 1 ? "Entrada" : "Salida";
		if (typeof tipo === "string") {
			const t = tipo.toLowerCase();
			if (t.startsWith("e")) tipo = "Entrada";
			else if (t.startsWith("s")) tipo = "Salida";
			else if (t === "entrada" || t === "salida") tipo = t[0].toUpperCase() + t.slice(1);
		}

		const idProducto = m?.idProducto ?? m?.IdProducto ?? null;
		const idMotivo = m?.idMotivo ?? m?.IdMotivo ?? null;
		const idUsuario = m?.idUsuario ?? m?.IdUsuario ?? null;

		const productoNombre =
			m?.productoNombre ??
			m?.ProductoNombre ??
			m?.producto?.nombre ??
			m?.Producto?.Nombre ??
			"-";

		const motivoNombre =
			m?.motivoNombre ??
			m?.MotivoNombre ??
			m?.motivo?.nombre ??
			m?.motivo?.descripcion ??
			m?.Motivo?.Nombre ??
			"";

		const usuarioNombre =
			m?.usuarioNombre ??
			m?.UsuarioNombre ??
			m?.usuario?.nombreCompleto ??
			m?.Usuario?.NombreCompleto ??
			m?.usuario?.username ??
			"";

		return {
			...m,
			idMovimiento: m?.idMovimiento ?? m?.IdMovimiento ?? m?.id ?? null,
			fecha: fecha ? normalizeDateString(fecha) : "",
			tipo,
			idProducto: idProducto != null ? Number(idProducto) : null,
			productoNombre: String(productoNombre ?? "-").trim(),
			cantidad: Number(m?.cantidad ?? m?.Cantidad ?? 0),
			idMotivo: idMotivo != null ? Number(idMotivo) : null,
			motivoNombre: String(motivoNombre ?? "").trim(),
			documento: m?.documento ?? m?.Documento ?? "",
			idUsuario: idUsuario != null ? Number(idUsuario) : null,
			usuarioNombre: String(usuarioNombre ?? "").trim(),
			createdAt: m?.createdAt ?? m?.CreatedAt ?? null,
		};
	}

	function normalizeDateString(v) {
		if (typeof v === "string") return v.slice(0, 10);
		try { return new Date(v).toISOString().slice(0, 10); } catch { return String(v).slice(0, 10); }
	}

	function motivoLabel(m) {
		const txt = m?.motivoNombre;
		if (txt && String(txt).trim()) return String(txt).trim();
		const id = Number(m?.idMotivo);
		if (!id) return "-";
		const found = motivos.value.find((x) => Number(x.idMotivo) === id);
		return found?.nombre ?? "-";
	}

	function usuarioLabel(m) {
		const txt = m?.usuarioNombre;
		if (txt && String(txt).trim()) return String(txt).trim();
		const id = Number(m?.idUsuario);
		if (!id) return "-";
		const found = usuarios.value.find((x) => Number(x.idUsuario) === id);
		return found?.nombreCompleto ?? "-";
	}

	async function fetchFirstList(endpoints) {
		let lastErr = null;

		for (const url of endpoints) {
			try {
				const data = await apiFetch(url);
				const list = normalizeList(data);
				return { url, list };
			} catch (e) {
				lastErr = e;
			}
		}

		throw (lastErr ?? new Error("No se pudo cargar la lista."));
	}

	async function loadProductos() {
		productosLoading.value = true;
		productosLoadedOnce.value = true;
		productosError.value = "";
		try {
			const data = await apiFetch(PRODUCTOS_ENDPOINT);
			const list = normalizeList(data);

			productos.value = list.map(normalizeProducto).filter((p) => p.idProducto != null && p.nombre);

			if (productos.value.length > 0 && form.idProducto == null) {
				form.idProducto = Number(productos.value[0].idProducto);
			}
		} catch (e) {
			productos.value = [];
			productosError.value = e?.message ?? "No se pudieron cargar los productos.";
		} finally {
			productosLoading.value = false;
		}
	}

	async function loadMotivos() {
		motivosLoading.value = true;
		motivosLoadedOnce.value = true;
		motivosError.value = "";
		try {
			const { list } = await fetchFirstList(MOTIVOS_ENDPOINTS);
			motivos.value = list.map(normalizeMotivo).filter((m) => m.idMotivo != null && m.nombre);

			if (motivos.value.length > 0 && form.idMotivo == null) {
				form.idMotivo = Number(motivos.value[0].idMotivo);
			}

			syncTipoConMotivo();
		} catch (e) {
			motivos.value = [];
			motivosError.value = e?.message ?? "No se pudieron cargar los motivos.";
		} finally {
			motivosLoading.value = false;
		}
	}

	async function loadUsuarios() {
		usuariosLoading.value = true;
		usuariosLoadedOnce.value = true;
		usuariosError.value = "";
		try {
			const { list } = await fetchFirstList(USUARIOS_ENDPOINTS);
			usuarios.value = list.map(normalizeUsuario).filter((u) => u.idUsuario != null && u.nombreCompleto);

			if (usuarios.value.length > 0 && form.idUsuario == null) {
				form.idUsuario = Number(usuarios.value[0].idUsuario);
			}
		} catch (e) {
			usuarios.value = [];
			usuariosError.value = e?.message ?? "No se pudieron cargar los usuarios.";
		} finally {
			usuariosLoading.value = false;
		}
	}

	async function loadMovimientos() {
		const data = await apiFetch(MOV_ENDPOINT);
		const list = normalizeList(data);
		rows.value = list.map(normalizeMovement);
	}

	const totalMovimientos = computed(() => rows.value.length);
	const totalEntradas = computed(() => rows.value.reduce((a, x) => a + (x.tipo === "Entrada" ? Number(x.cantidad) : 0), 0));
	const totalSalidas = computed(() => rows.value.reduce((a, x) => a + (x.tipo === "Salida" ? Number(x.cantidad) : 0), 0));

	const filteredRows = computed(() => {
		let list = rows.value;

		if (filterTipo.value === "entrada") list = list.filter((m) => m.tipo === "Entrada");
		if (filterTipo.value === "salida") list = list.filter((m) => m.tipo === "Salida");

		const q = search.value.trim().toLowerCase();
		if (!q) return list;

		return list.filter((m) => {
			return (
				String(m.fecha ?? "").toLowerCase().includes(q) ||
				String(m.tipo ?? "").toLowerCase().includes(q) ||
				String(m.productoNombre ?? "").toLowerCase().includes(q) ||
				String(motivoLabel(m) ?? "").toLowerCase().includes(q) ||
				String(m.documento ?? "").toLowerCase().includes(q) ||
				String(usuarioLabel(m) ?? "").toLowerCase().includes(q)
			);
		});
	});

	function rowKey(m) {
		return String(m?.idMovimiento ?? `${m?.fecha}-${m?.tipo}-${m?.idProducto}-${m?.cantidad}-${m?.idUsuario}`);
	}

	function openCreate() {
		if (!canEdit.value) return;
		apiError.value = "";
		Object.assign(form, emptyForm(), {
			idProducto: productos.value?.[0]?.idProducto ?? null,
			idMotivo: motivos.value?.[0]?.idMotivo ?? null,
			idUsuario: usuarios.value?.[0]?.idUsuario ?? null,
		});
		syncTipoConMotivo();
		isOpen.value = true;
	}

	function closeModal() {
		isOpen.value = false;
	}

	function validate() {
		if (!form.fecha) return "La fecha es obligatoria.";
		if (!form.tipo) return "El tipo es obligatorio.";
		if (!form.idProducto) return "Debes seleccionar un producto.";
		if (Number(form.cantidad) <= 0) return "La cantidad debe ser mayor que 0.";
		if (!form.idMotivo) return "Debes seleccionar un motivo.";
		if (!form.idUsuario) return "Debes seleccionar un usuario.";
		return "";
	}

	async function createMovement() {
		if (!canEdit.value) return;
		apiError.value = "";
		const err = validate();
		if (err) { apiError.value = err; return; }

		saving.value = true;
		try {
			const payload = {
				fecha: form.fecha,
				tipo: form.tipo,
				idProducto: Number(form.idProducto),
				cantidad: Number(form.cantidad),
				idMotivo: Number(form.idMotivo),
				documento: form.documento || null,
				idUsuario: Number(form.idUsuario),
			};

			const created = await apiFetch(MOV_ENDPOINT, {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify(payload),
			});

			if (created) rows.value.unshift(normalizeMovement(created));
			else await loadMovimientos();

			closeModal();
		} catch (e) {
			apiError.value = e?.message ?? "Error registrando el movimiento.";
		} finally {
			saving.value = false;
		}
	}

	function formatDate(v) {
		if (!v) return "";
		if (typeof v === "string" && v.length >= 10) return v.slice(0, 10);
		try { return new Date(v).toISOString().slice(0, 10); } catch { return String(v); }
	}

	function toDateInputValue(d) {
		const pad = (n) => String(n).padStart(2, "0");
		return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
	}
</script>

<style scoped>
	.page {
		min-height: 100vh;
		background: #eef3ff;
	}

	.content {
		padding: 22px;
	}

	.mutedLine {
		color: #64748b;
		font-weight: 800;
		padding: 10px 2px;
	}

	.miniWarn {
		margin-top: 8px;
		color: #b45309;
		font-weight: 800;
		font-size: 12px;
	}

	.miniHint {
		margin-top: 8px;
		color: #475569;
		font-weight: 800;
		font-size: 12px;
	}

	.hdr {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 6px;
		margin-bottom: 16px;
	}

	.hdr-left {
		display: flex;
		align-items: center;
		gap: 14px;
	}

	.cube {
		width: 44px;
		height: 44px;
		border-radius: 14px;
		background: rgba(59,130,246,.10);
		border: 1px solid rgba(59,130,246,.16);
		display: grid;
		place-items: center;
		color: #2563eb;
	}

		.cube svg {
			width: 22px;
			height: 22px;
		}

	.h1 {
		font-weight: 900;
		font-size: 26px;
		color: #0f172a;
	}

	.btn-primary {
		border: 0;
		cursor: pointer;
		padding: 12px 18px;
		border-radius: 12px;
		color: #fff;
		font-weight: 900;
		background: linear-gradient(180deg,#2f74ff,#1e5ae9);
		box-shadow: 0 14px 28px rgba(37,99,235,.25);
		display: flex;
		align-items: center;
		gap: 10px;
	}

	.plus {
		width: 18px;
		height: 18px;
		display: inline-grid;
		place-items: center;
		font-weight: 900;
	}

	.stats {
		display: grid;
		grid-template-columns: repeat(3,minmax(0,1fr));
		gap: 14px;
		margin-bottom: 16px;
	}

	.stat {
		background: rgba(255,255,255,.92);
		border: 1px solid rgba(15,23,42,.08);
		border-radius: 16px;
		box-shadow: 0 12px 24px rgba(10,20,70,.06);
		padding: 16px 18px;
		display: flex;
		align-items: center;
		gap: 14px;
	}

	.stat-ic {
		width: 42px;
		height: 42px;
		border-radius: 12px;
		display: grid;
		place-items: center;
	}

		.stat-ic svg {
			width: 22px;
			height: 22px;
		}

	.ic-blue {
		background: rgba(59,130,246,.12);
		color: #2563eb;
	}

	.ic-green {
		background: rgba(34,197,94,.12);
		color: #16a34a;
	}

	.ic-red {
		background: rgba(239,68,68,.12);
		color: #ef4444;
	}

	.stat-num {
		font-weight: 900;
		font-size: 28px;
		line-height: 1;
		color: #0f172a;
	}

		.stat-num.green {
			color: #16a34a;
		}

		.stat-num.red {
			color: #ef4444;
		}

	.stat-lbl {
		margin-top: 6px;
		font-weight: 800;
		color: #64748b;
		font-size: 13px;
	}

	.card {
		background: rgba(255,255,255,.92);
		border: 1px solid rgba(15,23,42,.08);
		border-radius: 16px;
		box-shadow: 0 16px 30px rgba(10,20,70,.08);
		overflow: hidden;
	}

	.toolbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 14px;
		padding: 14px 14px 10px;
	}

	.search {
		flex: 1;
		height: 46px;
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 0 14px;
		border-radius: 14px;
		background: rgba(248,250,252,.8);
		border: 1px solid rgba(15,23,42,.08);
	}

	.search-ic {
		opacity: .7;
		color: #64748b;
		display: grid;
		place-items: center;
	}

		.search-ic svg {
			width: 18px;
			height: 18px;
		}

	.search-in {
		border: 0;
		outline: none;
		width: 100%;
		font-size: 14px;
		background: transparent;
		color: #0f172a;
	}

	.tabs {
		display: flex;
		gap: 10px;
	}

	.tab {
		border: 1px solid rgba(15,23,42,.10);
		background: rgba(248,250,252,.9);
		color: #334155;
		font-weight: 900;
		padding: 10px 16px;
		border-radius: 12px;
		cursor: pointer;
	}

		.tab.active {
			background: linear-gradient(180deg,#2f74ff,#1e5ae9);
			color: #fff;
			border-color: transparent;
			box-shadow: 0 12px 22px rgba(37,99,235,.20);
		}

	.table {
		padding: 0 14px 10px;
	}

	.thead {
		display: grid;
		grid-template-columns: 1.1fr 0.9fr 1.8fr 0.8fr 1.1fr 1.2fr 1.1fr;
		gap: 14px;
		padding: 12px 12px;
		color: #64748b;
		font-weight: 900;
		font-size: 12px;
		border-top: 1px solid rgba(15,23,42,.06);
		border-bottom: 1px solid rgba(15,23,42,.06);
		background: rgba(248,250,252,.7);
		border-radius: 12px;
	}

	.trow {
		display: grid;
		grid-template-columns: 1.1fr 0.9fr 1.8fr 0.8fr 1.1fr 1.2fr 1.1fr;
		gap: 14px;
		padding: 16px 12px;
		border-bottom: 1px solid rgba(15,23,42,.05);
		align-items: center;
		font-size: 13px;
	}

	.date {
		font-weight: 900;
		color: #334155;
	}

	.prod {
		font-weight: 800;
		color: #0f172a;
	}

	.muted {
		color: #64748b;
		font-weight: 800;
	}

	.num {
		text-align: left;
	}

	.qty {
		font-weight: 900;
		color: #0f172a;
	}

	.pill {
		display: inline-flex;
		align-items: center;
		gap: 8px;
		padding: 7px 12px;
		border-radius: 999px;
		font-weight: 900;
		font-size: 12px;
		border: 1px solid transparent;
	}

		.pill .dot {
			width: 8px;
			height: 8px;
			border-radius: 99px;
			background: currentColor;
			opacity: .85;
		}

	.pill-green {
		color: #16a34a;
		background: rgba(34,197,94,.12);
		border-color: rgba(34,197,94,.18);
	}

	.pill-red {
		color: #ef4444;
		background: rgba(239,68,68,.10);
		border-color: rgba(239,68,68,.18);
	}

	.doc {
		display: flex;
		align-items: center;
		gap: 8px;
		color: #2563eb;
		font-weight: 900;
	}

	.doc-ic {
		width: 18px;
		height: 18px;
		display: grid;
		place-items: center;
	}

		.doc-ic svg {
			width: 18px;
			height: 18px;
		}

	.doc-txt {
		color: #2563eb;
	}

	.tfoot {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 14px 12px 6px;
		color: #64748b;
		font-weight: 800;
		font-size: 13px;
	}

	.modalOverlay {
		position: fixed;
		inset: 0;
		background: rgba(15,23,42,.25);
		display: grid;
		place-items: center;
		padding: 24px;
		z-index: 2000;
	}

	.modal {
		width: 860px;
		max-width: calc(100vw - 32px);
		background: #fff;
		border-radius: 0;
		box-shadow: 0 18px 40px rgba(0,0,0,.22);
		border: 1px solid rgba(15,23,42,.10);
		overflow: hidden;
	}

	.modalHead {
		height: 64px;
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0 18px;
		border-bottom: 1px solid rgba(15,23,42,.10);
	}

	.modalTitle {
		font-weight: 600;
		font-size: 26px;
		color: #0f172a;
	}

	.xBtn {
		width: 40px;
		height: 40px;
		border-radius: 10px;
		border: 0;
		background: transparent;
		cursor: pointer;
		font-size: 22px;
		line-height: 1;
		color: #0f172a;
	}

	.modalBody {
		padding: 18px;
		display: flex;
		flex-direction: column;
		gap: 14px;
	}

	.grid2 {
		display: grid;
		grid-template-columns: minmax(0,1fr) minmax(0,1fr);
		column-gap: 22px;
		row-gap: 14px;
	}

	.field label {
		display: block;
		margin-bottom: 8px;
		font-weight: 800;
		color: #64748b;
	}

	.field input, .field select, .field textarea {
		width: 100%;
		box-sizing: border-box;
		border: 1px solid rgba(148,163,184,.55);
		border-radius: 10px;
		padding: 12px 14px;
		font-size: 14px;
		outline: none;
		background: #fff;
		transition: border-color .15s ease, box-shadow .15s ease;
	}

	.field select {
		appearance: none;
		background-image: linear-gradient(45deg, transparent 50%, #64748b 50%), linear-gradient(135deg, #64748b 50%, transparent 50%);
		background-position: calc(100% - 18px) calc(50% + 1px), calc(100% - 12px) calc(50% + 1px);
		background-size: 6px 6px, 6px 6px;
		background-repeat: no-repeat;
	}

		.field input:focus, .field select:focus, .field textarea:focus {
			border-color: rgba(59,130,246,.65);
			box-shadow: 0 0 0 3px rgba(59,130,246,.18);
		}

	.modalFoot {
		padding: 14px 18px 18px;
		display: flex;
		justify-content: flex-end;
		align-items: center;
		gap: 18px;
	}

	.btnLink {
		border: 0;
		background: transparent;
		color: #64748b;
		font-weight: 900;
		cursor: pointer;
		padding: 10px 14px;
	}

	.btnPrimary {
		border: 0;
		cursor: pointer;
		padding: 12px 18px;
		border-radius: 12px;
		color: #fff;
		font-weight: 900;
		background: linear-gradient(180deg,#2f74ff,#1e5ae9);
		box-shadow: 0 14px 28px rgba(37,99,235,.25);
	}

		.btnPrimary:disabled {
			opacity: .7;
			cursor: not-allowed;
		}

	.alert {
		border: 1px solid rgba(239,68,68,.25);
		background: rgba(239,68,68,.08);
		color: #b91c1c;
		padding: 10px 12px;
		border-radius: 10px;
		font-weight: 700;
	}

	@media (max-width: 980px) {
		.grid2 {
			grid-template-columns: 1fr;
		}

		.toolbar {
			flex-direction: column;
			align-items: stretch;
		}

		.tabs {
			justify-content: flex-end;
		}
	}

	@media (max-width: 1100px) {
		.stats {
			grid-template-columns: 1fr;
		}

		.thead, .trow {
			grid-template-columns: 1fr;
		}
	}
</style>