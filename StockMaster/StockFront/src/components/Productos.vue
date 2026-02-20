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
					<div>
						<div class="h1">Gestión de Productos</div>
					</div>
				</div>

				<button class="btn-primary" type="button" @click="openCreate">
					<span class="plus">＋</span>
					Nuevo Producto
				</button>
			</div>

			<div class="search">
				<div class="search-ic">🔍</div>
				<input v-model="search" class="search-in" placeholder="Buscar productos..." />
			</div>

			<div class="stats">
				<div class="stat stat-blue">
					<div class="stat-num">{{ totalProductos }}</div>
					<div class="stat-lbl">Total de Productos</div>
				</div>

				<div class="stat stat-green">
					<div class="stat-num">{{ totalStock }}</div>
					<div class="stat-lbl">Unidades en Stock</div>
				</div>

				<div class="stat stat-red">
					<div class="stat-num">{{ stockBajo }}</div>
					<div class="stat-lbl">Stock Bajo</div>
				</div>
			</div>

			<div class="card">
				<div class="table">
					<div class="thead">
						<div>Código</div>
						<div>Producto</div>
						<div>Categoría</div>
						<div>Proveedor</div>
						<div class="num">P. Compra</div>
						<div class="num">P. Venta</div>
						<div class="num">Stock</div>
						<div class="actions-h">Acciones</div>
					</div>

					<!-- ✅ si no hay data real -->
					<div v-if="!loading && filteredRows.length === 0" class="empty">
						No hay productos. Crea uno con “Nuevo Producto”.
					</div>

					<div class="trow" v-for="p in visibleRows" :key="rowKey(p)">
						<div class="code">{{ p.codigo }}</div>

						<div class="prod">
							<div class="pname">{{ p.nombre }}</div>
							<div class="pdesc">{{ p.descripcion }}</div>
						</div>

						<div class="muted">{{ categoriaNombre(p) }}</div>
						<div class="muted">{{ proveedorNombre(p) }}</div>

						<div class="num price">{{ money(p.precioCompra) }}</div>
						<div class="num price">{{ money(p.precioVenta) }}</div>

						<div class="num" :class="Number(p.stockActual) <= Number(p.stockMinimo) ? 'stock-low' : 'stock-ok'">
							{{ p.stockActual }}
						</div>

						<div class="actions">
							<button class="icon-btn edit" type="button" title="Editar" aria-label="Editar" @click="openEdit(p)">✎</button>
							<button class="icon-btn del" type="button" title="Eliminar" aria-label="Eliminar" @click="removeProduct(p)">🗑</button>
						</div>
					</div>

					<div class="tfoot">
						<div class="foot-left">
							Mostrando {{ visibleRows.length }} de {{ filteredRows.length }} productos
						</div>

						<button class="foot-right" type="button" @click="toggleViewAll" :disabled="filteredRows.length <= pageSize">
							{{ viewAll ? "Ver Menos" : "Ver Todos" }} <span class="arrow">›</span>
						</button>
					</div>
				</div>
			</div>

			<!-- MODAL -->
			<div v-if="isOpen" class="modalOverlay" @click.self="closeModal">
				<div class="modal" role="dialog" aria-modal="true">
					<div class="modalHead">
						<div class="modalTitle">
							{{ mode === "create" ? "Nuevo Producto" : "Editar Producto" }}
						</div>
						<button class="xBtn" type="button" @click="closeModal" aria-label="Cerrar">×</button>
					</div>

					<div class="modalBody">
						<div v-if="apiError" class="alert">{{ apiError }}</div>

						<div class="grid2">
							<div class="field">
								<label>Código</label>
								<input v-model.trim="form.codigo" autocomplete="off" />
							</div>

							<div class="field">
								<label>Nombre</label>
								<input v-model.trim="form.nombre" autocomplete="off" />
							</div>
						</div>

						<div class="field">
							<label>Descripción</label>
							<textarea v-model.trim="form.descripcion" rows="4"></textarea>
						</div>

						<div class="grid2">
							<div class="field">
								<label>ID Categoría</label>
								<input type="number" min="1" step="1" v-model.number="form.idCategoria" />
							</div>

							<div class="field">
								<label>ID Proveedor</label>
								<input type="number" min="1" step="1" v-model.number="form.idProveedor" />
							</div>
						</div>

						<div class="grid2">
							<div class="field">
								<label>Precio Compra</label>
								<input type="number" step="0.01" min="0" v-model.number="form.precioCompra" />
							</div>

							<div class="field">
								<label>Precio Venta</label>
								<input type="number" step="0.01" min="0" v-model.number="form.precioVenta" />
							</div>
						</div>

						<div class="grid2">
							<div class="field">
								<label>Stock Actual</label>
								<input type="number" min="0" step="1" v-model.number="form.stockActual" />
							</div>

							<div class="field">
								<label>Stock Mínimo</label>
								<input type="number" min="0" step="1" v-model.number="form.stockMinimo" />
							</div>
						</div>
					</div>

					<div class="modalFoot">
						<button class="btnLink" type="button" @click="closeModal">Cancelar</button>

						<button class="btnPrimary" type="button" :disabled="saving" @click="saveProduct">
							{{ saving ? (mode === "create" ? "Creando..." : "Guardando...") : (mode === "create" ? "Crear Producto" : "Guardar Cambios") }}
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
	const PRODUCTS_ENDPOINT = `${API_BASE}/api/Productos`;

	const search = ref("");
	const isOpen = ref(false);
	const saving = ref(false);
	const loading = ref(false);
	const apiError = ref("");

	const viewAll = ref(false);
	const pageSize = 3;

	const mode = ref("create"); // "create" | "edit"
	const editingIdProducto = ref(null);

	const rows = ref([]);

	const emptyForm = () => ({
		idProducto: null,
		codigo: "",
		nombre: "",
		descripcion: "",
		idCategoria: 0,
		idProveedor: 0,
		precioCompra: 0,
		precioVenta: 0,
		stockActual: 0,
		stockMinimo: 0,
	});
	const form = reactive(emptyForm());

	onMounted(loadProducts);

	async function loadProducts() {
		loading.value = true;
		apiError.value = "";
		try {
			const res = await fetch(PRODUCTS_ENDPOINT);
			if (!res.ok) throw new Error(`GET /api/Productos falló (${res.status})`);

			const data = await res.json();
			const list = Array.isArray(data) ? data : (data?.items ?? []);

			rows.value = list;
		} catch (e) {
			rows.value = [];
			apiError.value = e?.message ?? "No se pudo cargar productos desde la API.";
		} finally {
			loading.value = false;
		}
	}

	function rowKey(p) {
		return String(p?.idProducto ?? p?.codigo ?? Math.random());
	}

	function categoriaNombre(p) {
		return p?.categoria?.nombre ?? (p?.idCategoria ? `ID ${p.idCategoria}` : "");
	}

	function proveedorNombre(p) {
		return p?.proveedor?.nombreEmpresa ?? (p?.idProveedor ? `ID ${p.idProveedor}` : "");
	}

	const filteredRows = computed(() => {
		const q = search.value.trim().toLowerCase();
		if (!q) return rows.value;

		return rows.value.filter((p) => {
			const cat = String(p?.categoria?.nombre ?? "").toLowerCase();
			const prov = String(p?.proveedor?.nombreEmpresa ?? "").toLowerCase();

			return (
				String(p.codigo ?? "").toLowerCase().includes(q) ||
				String(p.nombre ?? "").toLowerCase().includes(q) ||
				String(p.descripcion ?? "").toLowerCase().includes(q) ||
				cat.includes(q) ||
				prov.includes(q)
			);
		});
	});

	const visibleRows = computed(() => {
		if (viewAll.value) return filteredRows.value;
		return filteredRows.value.slice(0, pageSize);
	});

	function toggleViewAll() {
		viewAll.value = !viewAll.value;
	}

	const totalProductos = computed(() => rows.value.length);
	const totalStock = computed(() => rows.value.reduce((acc, p) => acc + Number(p.stockActual ?? 0), 0));
	const stockBajo = computed(() => rows.value.reduce((acc, p) => acc + (Number(p.stockActual ?? 0) <= Number(p.stockMinimo ?? 0) ? 1 : 0), 0));

	function money(v) {
		return `$${Number(v ?? 0).toFixed(2)}`;
	}

	function openCreate() {
		apiError.value = "";
		mode.value = "create";
		editingIdProducto.value = null;
		Object.assign(form, emptyForm());
		isOpen.value = true;
	}

	function openEdit(p) {
		apiError.value = "";

		const id = p?.idProducto ?? null;
		if (!id) {
			apiError.value = "Este registro no tiene idProducto. Si lo ves, viene de datos no persistidos.";
			return;
		}

		mode.value = "edit";
		editingIdProducto.value = id;

		Object.assign(form, emptyForm(), {
			idProducto: id,
			codigo: p.codigo ?? "",
			nombre: p.nombre ?? "",
			descripcion: p.descripcion ?? "",
			idCategoria: Number(p.idCategoria ?? 0),
			idProveedor: Number(p.idProveedor ?? 0),
			precioCompra: Number(p.precioCompra ?? 0),
			precioVenta: Number(p.precioVenta ?? 0),
			stockActual: Number(p.stockActual ?? 0),
			stockMinimo: Number(p.stockMinimo ?? 0),
		});

		isOpen.value = true;
	}

	function closeModal() {
		isOpen.value = false;
	}

	function validate() {
		if (!form.codigo || !form.nombre) return "Código y Nombre son obligatorios.";
		if (Number(form.idCategoria) <= 0) return "ID Categoría debe ser > 0.";
		if (Number(form.idProveedor) <= 0) return "ID Proveedor debe ser > 0.";
		if (Number(form.precioCompra) < 0 || Number(form.precioVenta) < 0) return "Los precios no pueden ser negativos.";
		if (Number(form.stockActual) < 0 || Number(form.stockMinimo) < 0) return "El stock no puede ser negativo.";
		return "";
	}

	async function readApiError(res) {
		let msg = `Error (${res.status}).`;
		try {
			const data = await res.json();
			msg = data.message || data.msg || data.error || JSON.stringify(data);
		} catch { }
		return new Error(msg);
	}

	async function saveProduct() {
		apiError.value = "";
		const err = validate();
		if (err) {
			apiError.value = err;
			return;
		}

		saving.value = true;
		try {
			const payload = {
				codigo: form.codigo,
				nombre: form.nombre,
				descripcion: form.descripcion,
				idCategoria: Number(form.idCategoria),
				idProveedor: Number(form.idProveedor),
				precioCompra: Number(form.precioCompra),
				precioVenta: Number(form.precioVenta),
				stockActual: Number(form.stockActual),
				stockMinimo: Number(form.stockMinimo),
			};

			// CREATE
			if (mode.value === "create") {
				const res = await fetch(PRODUCTS_ENDPOINT, {
					method: "POST",
					headers: { "Content-Type": "application/json" },
					body: JSON.stringify(payload),
				});
				if (!res.ok) throw await readApiError(res);

				let created = null;
				try {
					created = await res.json();
				} catch {
					created = null;
				}

				// si API no devuelve el objeto, recargamos lista
				if (!created || !created.idProducto) {
					await loadProducts();
				} else {
					rows.value.unshift(created);
				}

				closeModal();
				return;
			}

			// EDIT
			if (!editingIdProducto.value) {
				apiError.value = "No hay idProducto para editar.";
				return;
			}

			const url = `${PRODUCTS_ENDPOINT}/${encodeURIComponent(editingIdProducto.value)}`;
			const res = await fetch(url, {
				method: "PUT",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify(payload),
			});
			if (!res.ok) throw await readApiError(res);

			// muchos PUT devuelven 204 No Content: en ese caso recargamos
			if (res.status === 204) {
				await loadProducts();
				closeModal();
				return;
			}

			let updated = null;
			try {
				updated = await res.json();
			} catch {
				updated = { ...payload, idProducto: editingIdProducto.value };
			}

			const idx = rows.value.findIndex((r) => Number(r?.idProducto) === Number(editingIdProducto.value));
			if (idx !== -1) rows.value[idx] = { ...rows.value[idx], ...updated, ...payload };

			closeModal();
		} catch (e) {
			apiError.value = e?.message ?? "Error guardando el producto.";
		} finally {
			saving.value = false;
		}
	}

	async function removeProduct(p) {
		const id = p?.idProducto ?? null;
		const name = p?.nombre ?? p?.codigo ?? "este producto";

		if (!id) {
			alert("Este registro no tiene idProducto. No se puede eliminar.");
			return;
		}

		if (!confirm(`¿Seguro que deseas eliminar ${name}?`)) return;

		try {
			const url = `${PRODUCTS_ENDPOINT}/${encodeURIComponent(id)}`;
			const res = await fetch(url, { method: "DELETE" });
			if (!res.ok) throw await readApiError(res);

			rows.value = rows.value.filter((r) => Number(r?.idProducto) !== Number(id));
		} catch (e) {
			alert(e?.message ?? "No se pudo eliminar.");
		}
	}
</script>

<style scoped>
	/* TU CSS queda igual (lo dejé intacto) */
	.page {
		min-height: 100vh;
		background: #eef3ff;
	}

	.content {
		padding: 22px;
	}

	.hdr {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 6px;
		margin-bottom: 14px;
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
		background: rgba(59, 130, 246, 0.10);
		border: 1px solid rgba(59, 130, 246, 0.16);
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
		background: linear-gradient(180deg, #2f74ff, #1e5ae9);
		box-shadow: 0 14px 28px rgba(37, 99, 235, 0.25);
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

	.search {
		height: 48px;
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 0 14px;
		border-radius: 14px;
		background: rgba(255, 255, 255, 0.92);
		border: 1px solid rgba(15, 23, 42, 0.08);
		box-shadow: 0 10px 22px rgba(10, 20, 70, 0.06);
		margin-bottom: 14px;
	}

	.search-ic {
		opacity: 0.75;
	}

	.search-in {
		border: 0;
		outline: none;
		width: 100%;
		font-size: 14px;
		background: transparent;
		color: #0f172a;
	}

	.stats {
		display: grid;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		gap: 14px;
		margin-bottom: 16px;
	}

	.stat {
		border-radius: 14px;
		padding: 16px 18px;
		border: 1px solid rgba(15, 23, 42, 0.06);
		box-shadow: 0 12px 24px rgba(10, 20, 70, 0.06);
	}

	.stat-num {
		font-weight: 900;
		font-size: 28px;
	}

	.stat-lbl {
		margin-top: 6px;
		font-weight: 800;
		color: #334155;
		font-size: 13px;
	}

	.stat-blue {
		background: rgba(59, 130, 246, 0.12);
	}

		.stat-blue .stat-num {
			color: #2563eb;
		}

	.stat-green {
		background: rgba(34, 197, 94, 0.10);
	}

		.stat-green .stat-num {
			color: #16a34a;
		}

	.stat-red {
		background: rgba(239, 68, 68, 0.10);
	}

		.stat-red .stat-num {
			color: #ef4444;
		}

	.card {
		background: rgba(255, 255, 255, 0.92);
		border: 1px solid rgba(15, 23, 42, 0.08);
		border-radius: 16px;
		box-shadow: 0 16px 30px rgba(10, 20, 70, 0.08);
		overflow: hidden;
	}

	.table {
		padding: 12px 14px 10px;
	}

	.thead {
		display: grid;
		grid-template-columns: 1fr 1.4fr 1.2fr 1.1fr 0.8fr 0.8fr 0.6fr 0.7fr;
		gap: 14px;
		padding: 12px 12px;
		color: #64748b;
		font-weight: 900;
		font-size: 12px;
		border-bottom: 1px solid rgba(15, 23, 42, 0.06);
		background: rgba(248, 250, 252, 0.7);
		border-radius: 12px;
	}

	.trow {
		display: grid;
		grid-template-columns: 1fr 1.4fr 1.2fr 1.1fr 0.8fr 0.8fr 0.6fr 0.7fr;
		gap: 14px;
		padding: 16px 12px;
		border-bottom: 1px solid rgba(15, 23, 42, 0.05);
		align-items: center;
		font-size: 13px;
	}

	.code {
		font-weight: 900;
		color: #1e293b;
	}

	.prod .pname {
		font-weight: 900;
		color: #0f172a;
	}

	.prod .pdesc {
		margin-top: 4px;
		font-weight: 700;
		font-size: 12px;
		color: #94a3b8;
	}

	.muted {
		color: #64748b;
		font-weight: 800;
		font-size: 13px;
	}

	.num {
		text-align: right;
	}

	.price {
		font-weight: 900;
		color: #0f172a;
	}

	.stock-low {
		font-weight: 900;
		color: #ef4444;
	}

	.stock-ok {
		font-weight: 900;
		color: #0f172a;
	}

	.actions-h {
		text-align: right;
	}

	.actions {
		display: flex;
		justify-content: flex-end;
		gap: 10px;
	}

	.icon-btn {
		width: 34px;
		height: 34px;
		border-radius: 10px;
		border: 1px solid rgba(15, 23, 42, 0.10);
		background: rgba(255, 255, 255, 0.95);
		cursor: pointer;
		display: grid;
		place-items: center;
		box-shadow: 0 10px 18px rgba(10, 20, 70, 0.06);
	}

		.icon-btn.edit {
			color: #2563eb;
		}

		.icon-btn.del {
			color: #ef4444;
		}

	.tfoot {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 14px 12px 6px;
	}

	.foot-left {
		color: #64748b;
		font-weight: 800;
		font-size: 13px;
	}

	.foot-right {
		border: 0;
		background: transparent;
		color: #2563eb;
		font-weight: 900;
		cursor: pointer;
		display: flex;
		align-items: center;
		gap: 6px;
	}

		.foot-right:disabled {
			opacity: .55;
			cursor: not-allowed;
		}

	.arrow {
		font-size: 18px;
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
		width: 820px;
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
		font-weight: 500;
		font-size: 22px;
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

	.field {
		min-width: 0;
	}

		.field label {
			display: block;
			margin-bottom: 8px;
			font-weight: 800;
			color: #64748b;
		}

		.field input, .field textarea {
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

		.field textarea {
			resize: vertical;
			min-height: 110px;
		}

			.field input:focus, .field textarea:focus {
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
		border: 1px solid rgba(239, 68, 68, 0.25);
		background: rgba(239, 68, 68, 0.08);
		color: #b91c1c;
		padding: 10px 12px;
		border-radius: 10px;
		font-weight: 700;
	}

	.empty {
		padding: 18px 12px;
		color: #64748b;
		font-weight: 800;
	}

	@media (max-width: 980px) {
		.grid2 {
			grid-template-columns: 1fr;
		}
	}

	@media (max-width: 1100px) {
		.stats {
			grid-template-columns: 1fr;
		}

		.thead, .trow {
			grid-template-columns: 1fr;
		}

		.num, .actions-h {
			text-align: left;
		}

		.actions {
			justify-content: flex-start;
		}
	}
</style>
