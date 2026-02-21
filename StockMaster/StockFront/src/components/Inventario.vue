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

				<button class="btn-primary" type="button" @click="openCreate">
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
						<div>Responsable</div>
					</div>

					<div class="trow" v-for="m in filteredRows" :key="rowKey(m)">
						<div class="date">{{ formatDate(m.fecha) }}</div>

						<div>
							<span class="pill" :class="m.tipo === 'Entrada' ? 'pill-green' : 'pill-red'">
								<span class="dot"></span>
								{{ m.tipo }}
							</span>
						</div>

						<div class="prod">{{ m.producto }}</div>
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

						<div class="muted">{{ m.responsable }}</div>
					</div>

					<div class="tfoot">
						<div class="foot-left">Mostrando {{ filteredRows.length }} de {{ rows.length }} movimientos</div>
						<div class="foot-right">Mostrando {{ filteredRows.length }} de {{ rows.length }} movimientos</div>
					</div>
				</div>
			</div>

			<!-- MODAL -->
			<div v-if="isOpen" class="modalOverlay" @click.self="closeModal">
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
								<input type="date" v-model="form.fecha" />
							</div>

							<div class="field">
								<label>Tipo de Movimiento</label>
								<select v-model="form.tipo">
									<option value="Entrada">Entrada</option>
									<option value="Salida">Salida</option>
								</select>
							</div>
						</div>

						<div class="field">
							<label>Producto</label>
							<input v-model.trim="form.producto" placeholder="Nombre del producto" autocomplete="off" />
						</div>

						<div class="grid2">
							<div class="field">
								<label>Cantidad</label>
								<input type="number" min="0" step="1" v-model.number="form.cantidad" />
							</div>

							<div class="field">
								<label>Documento (Opcional)</label>
								<input v-model.trim="form.documento" placeholder="Ej: FAC-2024-001" autocomplete="off" />
							</div>
						</div>

						<div class="field">
							<label>Motivo</label>
							<select v-model.number="form.idMotivoMovimiento" :disabled="motivosLoading">
								<option :value="null" disabled>
									{{ motivosLoading ? "Cargando motivos..." : "Seleccione un motivo" }}
								</option>

								<option v-for="mm in motivos" :key="String(mm.idMotivoMovimiento)" :value="Number(mm.idMotivoMovimiento)">
									{{ mm.descripcion }}
								</option>
							</select>

							<div v-if="!motivosLoading && motivosLoadedOnce && motivos.length === 0" class="miniWarn">
								No hay motivos registrados en la base de datos (api/MotivosMovimiento devolvió vacío).
							</div>
							<div v-else-if="motivosError" class="miniWarn">{{ motivosError }}</div>
						</div>

						<div class="field">
							<label>Responsable</label>
							<input v-model.trim="form.responsable" placeholder="Ej: Admin" autocomplete="off" />
						</div>
					</div>

					<div class="modalFoot">
						<button class="btnLink" type="button" @click="closeModal">Cancelar</button>

						<button class="btnPrimary" type="button" :disabled="saving || motivosLoading" @click="createMovement">
							{{ saving ? "Registrando..." : "Registrar Movimiento" }}
						</button>
					</div>
				</div>
			</div>
			<!-- /MODAL -->
		</div>
	</div>
</template>

<script setup>
	import { computed, onMounted, reactive, ref } from "vue";

	const API_BASE = "https://localhost:7198";
	const MOV_ENDPOINT = `${API_BASE}/api/Movimientos`;

	const MOTIVOS_ENDPOINTS = [
		`${API_BASE}/api/MotivosMovimiento`,
		`${API_BASE}/api/MotivoMovimiento`,
		`${API_BASE}/api/MotivosMovimientos`,
		`${API_BASE}/api/MotivosInventario`,
		`${API_BASE}/api/InventarioMotivos`,
	];

	const search = ref("");
	const filterTipo = ref("todos");

	const loading = ref(false);
	const loadError = ref("");

	const isOpen = ref(false);
	const saving = ref(false);
	const apiError = ref("");

	const rows = ref([]);

	const motivos = ref([]);
	const motivosError = ref("");

	const motivosLoading = ref(false);
	const motivosLoadedOnce = ref(false);

	const motivosEndpointUsado = ref("");

	const emptyForm = () => ({
		fecha: toDateInputValue(new Date()),
		tipo: "Entrada",
		producto: "",
		cantidad: 0,
		idMotivoMovimiento: null,
		documento: "",
		responsable: "Admin",
	});
	const form = reactive(emptyForm());

	onMounted(async () => {
		await loadAll();
	});

	async function loadAll() {
		loading.value = true;
		loadError.value = "";
		try {
			await loadMotivos();
			await loadMovimientos();
		} catch (e) {
			loadError.value = e?.message ?? "Error cargando inventario.";
		} finally {
			loading.value = false;
		}
	}

	/** =======================
	 * NORMALIZERS
	 * ======================= */

	// ✅ AGREGADO: soporte $values (muy típico en .NET si ReferenceHandler.Preserve)
	function normalizeList(data) {
		if (Array.isArray(data)) return data;

		// ASP.NET preserve references
		if (Array.isArray(data?.$values)) return data.$values;

		if (Array.isArray(data?.items)) return data.items;
		if (Array.isArray(data?.data)) return data.data;
		if (Array.isArray(data?.result)) return data.result;
		if (Array.isArray(data?.value)) return data.value;
		if (Array.isArray(data?.results)) return data.results;

		return [];
	}

	// ✅ FIX REAL: tu tabla trae IdMotivo, Nombre, TipoAplica
	function normalizeMotivo(x) {
		const idMotivoMovimiento =
			x?.idMotivoMovimiento ??
			x?.motivoMovimientoId ??
			x?.MotivoMovimientoId ??
			x?.IdMotivoMovimiento ??
			x?.IdMotivo ??           // ✅ NEW (SQL)
			x?.idMotivo ??           // ✅ NEW (posible DTO)
			x?.id ??
			x?.Id ??
			null;

		const descripcion =
			x?.descripcion ??
			x?.Descripcion ??
			x?.nombre ??
			x?.Nombre ??             // ✅ NEW (SQL)
			x?.motivo ??
			x?.Motivo ??
			"";

		// opcional por si luego filtras por tipo aplica
		const tipoAplica =
			x?.tipoAplica ??
			x?.TipoAplica ??
			x?.aplica ??
			x?.Aplica ??
			null;

		return {
			...x,
			idMotivoMovimiento: idMotivoMovimiento != null ? Number(idMotivoMovimiento) : null,
			descripcion: String(descripcion ?? "").trim(),
			tipoAplica,
		};
	}

	function normalizeMovement(m) {
		const fecha = m?.fecha ?? m?.fechaMovimiento ?? m?.createdAt ?? m?.date ?? null;

		let tipo = m?.tipo ?? m?.type ?? m?.tipoMovimiento ?? m?.Tipo ?? "Entrada";
		if (typeof tipo === "number") tipo = tipo === 1 ? "Entrada" : "Salida";
		if (typeof tipo === "string") {
			const t = tipo.toLowerCase();
			if (t.startsWith("e")) tipo = "Entrada";
			else if (t.startsWith("s")) tipo = "Salida";
			else if (t === "entrada" || t === "salida") tipo = t[0].toUpperCase() + t.slice(1);
		}

		const idMotivo =
			m?.idMotivoMovimiento ??
			m?.motivoMovimientoId ??
			m?.IdMotivoMovimiento ??
			m?.IdMotivo ??     // ✅ por si el backend manda IdMotivo
			m?.idMotivo ??
			m?.motivoId ??
			m?.idMotivo ??
			null;

		const motivoTxt =
			m?.motivo ??
			m?.motivoDescripcion ??
			m?.descripcionMotivo ??
			m?.Motivo ??
			m?.MotivoDescripcion ??
			null;

		return {
			...m,
			idMovimiento: m?.idMovimiento ?? m?.movimientoId ?? m?.IdMovimiento ?? m?.id ?? null,
			fecha: fecha ? normalizeDateString(fecha) : "",
			tipo,
			producto: m?.producto ?? m?.nombreProducto ?? m?.productName ?? m?.Producto ?? "",
			cantidad: Number(m?.cantidad ?? m?.qty ?? m?.Cantidad ?? 0),
			idMotivoMovimiento: idMotivo != null ? Number(idMotivo) : null,
			motivo: motivoTxt,
			documento: m?.documento ?? m?.doc ?? m?.Documento ?? "",
			responsable: m?.responsable ?? m?.user ?? m?.Responsable ?? "",
		};
	}

	function normalizeDateString(v) {
		if (typeof v === "string") return v.slice(0, 10);
		try { return new Date(v).toISOString().slice(0, 10); } catch { return String(v).slice(0, 10); }
	}

	/** =======================
	 * RESOLVER MOTIVO
	 * ======================= */
	function resolveMotivoDescripcion(idMotivo) {
		if (!idMotivo) return "";
		const found = motivos.value.find((x) => Number(x.idMotivoMovimiento) === Number(idMotivo));
		return found?.descripcion ?? "";
	}

	function motivoLabel(m) {
		const txt = m?.motivo;
		if (txt && String(txt).trim()) return String(txt).trim();
		return resolveMotivoDescripcion(m?.idMotivoMovimiento) || "-";
	}

	/** =======================
	 * HTTP HELPERS
	 * ======================= */
	async function readApiError(res) {
		let text = "";
		try {
			const ct = res.headers.get("content-type") || "";
			if (ct.includes("application/json")) {
				const data = await res.json();
				text =
					data?.message ||
					data?.msg ||
					data?.error ||
					data?.title ||
					(data?.errors ? JSON.stringify(data.errors) : "") ||
					JSON.stringify(data);
			} else {
				text = await res.text();
			}
		} catch {}

		const msg = text?.trim()
			? `${res.status} ${res.statusText}: ${text}`
			: `${res.status} ${res.statusText}`;

		return new Error(msg);
	}

	async function fetchFirstList(endpoints) {
		let lastErr = null;

		for (const url of endpoints) {
			try {
				const res = await fetch(url);

				if (!res.ok) {
					lastErr = await readApiError(res);
					continue;
				}

				if (res.status === 204) {
					return { url, list: [] };
				}

				const data = await res.json();
				const list = normalizeList(data);
				return { url, list };
			} catch (e) {
				lastErr = e;
			}
		}

		throw (lastErr ?? new Error("No se pudo cargar la lista."));
	}

	/** =======================
	 * LOADERS
	 * ======================= */
	async function loadMotivos() {
		motivosLoading.value = true;
		motivosLoadedOnce.value = true;
		motivosError.value = "";
		motivosEndpointUsado.value = "";

		try {
			const { url, list } = await fetchFirstList(MOTIVOS_ENDPOINTS);
			motivosEndpointUsado.value = url;

			// ✅ ya normaliza IdMotivo/Nombre y no “bota” los registros
			motivos.value = list
				.map(normalizeMotivo)
				.filter((x) => x.idMotivoMovimiento != null && String(x.descripcion ?? "").trim().length > 0);

			if (motivos.value.length > 0 && form.idMotivoMovimiento == null) {
				form.idMotivoMovimiento = Number(motivos.value[0].idMotivoMovimiento);
			}
		} catch (e) {
			motivos.value = [];
			motivosError.value = e?.message ?? "No se pudieron cargar los motivos.";
		} finally {
			motivosLoading.value = false;
		}
	}

	async function loadMovimientos() {
		const res = await fetch(MOV_ENDPOINT);
		if (!res.ok) throw await readApiError(res);

		const data = await res.json();
		const list = normalizeList(data);

		rows.value = list.map(normalizeMovement);
	}

	/** =======================
	 * KPIs
	 * ======================= */
	const totalMovimientos = computed(() => rows.value.length);
	const totalEntradas = computed(() =>
		rows.value.reduce((a, x) => a + (x.tipo === "Entrada" ? Number(x.cantidad) : 0), 0)
	);
	const totalSalidas = computed(() =>
		rows.value.reduce((a, x) => a + (x.tipo === "Salida" ? Number(x.cantidad) : 0), 0)
	);

	/** =======================
	 * FILTERS
	 * ======================= */
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
				String(m.producto ?? "").toLowerCase().includes(q) ||
				String(motivoLabel(m) ?? "").toLowerCase().includes(q) ||
				String(m.documento ?? "").toLowerCase().includes(q) ||
				String(m.responsable ?? "").toLowerCase().includes(q)
			);
		});
	});

	/** =======================
	 * UI
	 * ======================= */
	function rowKey(m) {
		return String(m?.idMovimiento ?? `${m?.fecha}-${m?.tipo}-${m?.producto}-${m?.cantidad}`);
	}

	function openCreate() {
		apiError.value = "";
		Object.assign(form, emptyForm(), {
			idMotivoMovimiento: motivos.value?.[0]?.idMotivoMovimiento ?? null,
		});
		isOpen.value = true;
	}

	function closeModal() {
		isOpen.value = false;
	}

	/** =======================
	 * VALIDATION
	 * ======================= */
	function validate() {
		if (!form.fecha) return "La fecha es obligatoria.";
		if (!form.tipo) return "El tipo es obligatorio.";
		if (!form.producto) return "El producto es obligatorio.";
		if (Number(form.cantidad) <= 0) return "La cantidad debe ser mayor que 0.";
		if (!form.idMotivoMovimiento) return "Debes seleccionar un motivo.";
		return "";
	}

	/** =======================
	 * CREATE
	 * ======================= */
	async function createMovement() {
		apiError.value = "";
		const err = validate();
		if (err) { apiError.value = err; return; }

		saving.value = true;
		try {
			const payload = {
				fecha: form.fecha,
				tipo: form.tipo,
				producto: form.producto,
				cantidad: Number(form.cantidad),

				// ✅ enviamos todas por compatibilidad
				idMotivoMovimiento: Number(form.idMotivoMovimiento),
				motivoMovimientoId: Number(form.idMotivoMovimiento),
				idMotivo: Number(form.idMotivoMovimiento),  // ✅ por si tu backend usa IdMotivo

				documento: form.documento || null,
				responsable: form.responsable || null,
			};

			const res = await fetch(MOV_ENDPOINT, {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify(payload),
			});

			if (!res.ok) throw await readApiError(res);

			let created = null;
			try { created = await res.json(); } catch { created = null; }

			if (created) rows.value.unshift(normalizeMovement(created));
			else await loadMovimientos();

			closeModal();
		} catch (e) {
			apiError.value = e?.message ?? "Error registrando el movimiento.";
		} finally {
			saving.value = false;
		}
	}

	/** =======================
	 * DATES
	 * ======================= */
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
	/* tu CSS original + 2 utilidades */
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

	/* ---- tu CSS (sin cambios de estilo, copiado tal cual) ---- */
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