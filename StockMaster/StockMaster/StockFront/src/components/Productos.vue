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

				<button v-if="canEdit" class="btn-primary" type="button" @click="openCreate">
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

					<div v-if="loading" class="empty">Cargando productos...</div>

					<div v-else-if="filteredRows.length === 0" class="empty">
						No hay productos. Crea uno con “Nuevo Producto”.
					</div>

					<div v-else class="trow" v-for="p in visibleRows" :key="rowKey(p)">
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
							{{ Number(p.stockActual ?? 0) }}
						</div>

						<div class="actions">
							<button v-if="canEdit" class="icon-btn edit" type="button" title="Editar" aria-label="Editar" @click="openEdit(p)">✎</button>
							<button v-if="canEdit" class="icon-btn del" type="button" title="Eliminar" aria-label="Eliminar" @click="removeProduct(p)">🗑</button>

							<span v-if="!canEdit" class="muted">Solo lectura</span>
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

			<div v-if="isOpen && canEdit" class="modalOverlay" @click.self="closeModal">
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
								<input v-model.trim="form.codigo" autocomplete="off" :disabled="!canEdit" />
							</div>

							<div class="field">
								<label>Nombre</label>
								<input v-model.trim="form.nombre" autocomplete="off" :disabled="!canEdit" />
							</div>
						</div>

						<div class="field">
							<label>Descripción</label>
							<textarea v-model.trim="form.descripcion" rows="4" :disabled="!canEdit"></textarea>
						</div>

						<div class="grid2">
							<div class="field">
								<label>Categoría</label>
								<select v-model.number="form.idCategoria" :disabled="categoriasLoading || !canEdit">
									<option :value="0" disabled>
										{{ categoriasLoading ? "Cargando categorías..." : "Seleccione una categoría" }}
									</option>
									<option v-for="c in categorias" :key="String(c.idCategoria)" :value="Number(c.idCategoria)">
										{{ c.nombre }}
									</option>
								</select>

								<div v-if="!categoriasLoading && categoriasLoadedOnce && categorias.length === 0" class="miniWarn">
									No hay categorías registradas (api/Categorias devolvió vacío).
								</div>
								<div v-else-if="categoriasError" class="miniWarn">{{ categoriasError }}</div>
							</div>

							<div class="field">
								<label>Proveedor</label>
								<select v-model.number="form.idProveedor" :disabled="proveedoresLoading || !canEdit">
									<option :value="0" disabled>
										{{ proveedoresLoading ? "Cargando proveedores..." : "Seleccione un proveedor" }}
									</option>
									<option v-for="p in proveedores" :key="String(p.idProveedor)" :value="Number(p.idProveedor)">
										{{ p.nombreEmpresa }}
									</option>
								</select>

								<div v-if="!proveedoresLoading && proveedoresLoadedOnce && proveedores.length === 0" class="miniWarn">
									No hay proveedores registrados (api/Proveedores devolvió vacío).
								</div>
								<div v-else-if="proveedoresError" class="miniWarn">{{ proveedoresError }}</div>
							</div>
						</div>

						<div class="grid2">
							<div class="field">
								<label>Precio Compra</label>
								<input type="number" step="0.01" min="0" inputmode="decimal" v-model.number="form.precioCompra" :disabled="!canEdit" />
							</div>

							<div class="field">
								<label>Precio Venta</label>
								<input type="number" step="0.01" min="0" inputmode="decimal" v-model.number="form.precioVenta" :disabled="!canEdit" />
							</div>
						</div>

						<div class="stockRow">
							<div class="field half">
								<label>Stock Actual</label>
								<input type="number"
									   min="0"
									   step="1"
									   inputmode="numeric"
									   autocomplete="off"
									   :value="Number(form.stockActual ?? 0)"
									   @input="form.stockActual = $event.target.value === '' ? 0 : $event.target.valueAsNumber"
									   :disabled="!canEdit" />
							</div>

							<div class="field half">
								<label>Stock Mínimo</label>
								<input type="number"
									   min="0"
									   step="1"
									   inputmode="numeric"
									   :value="Number(form.stockMinimo ?? 0)"
									   @input="form.stockMinimo = $event.target.value === '' ? 0 : $event.target.valueAsNumber"
									   :disabled="!canEdit" />
							</div>
						</div>
					</div>

					<div class="modalFoot">
						<button class="btnLink" type="button" @click="closeModal">Cancelar</button>

						<button class="btnPrimary" type="button" :disabled="saving || categoriasLoading || proveedoresLoading || !canEdit" @click="saveProduct">
							{{ saving ? (mode === "create" ? "Creando..." : "Guardando...") : (mode === "create" ? "Crear Producto" : "Guardar Cambios") }}
						</button>
					</div>
				</div>
			</div>

		</div>
	</div>
</template>

<script setup>
	import { computed, onMounted, reactive, ref } from "vue";
	import { getUser } from "../router/auth.service";
	import { getPermsSafe } from "../services/permissions";
	import { apiFetch } from "../services/api";

	const user = computed(() => getUser());
	const perms = computed(() => getPermsSafe(user.value));
	const canEdit = computed(() => perms.value?.canEditProductos === true || perms.value?.canEditAll === true);

	const search = ref("");
	const isOpen = ref(false);
	const saving = ref(false);
	const loading = ref(false);
	const apiError = ref("");

	const viewAll = ref(false);
	const pageSize = 3;

	const mode = ref("create");
	const editingIdProducto = ref(null);

	const rows = ref([]);

	const categorias = ref([]);
	const proveedores = ref([]);

	const categoriasLoading = ref(false);
	const proveedoresLoading = ref(false);

	const categoriasLoadedOnce = ref(false);
	const proveedoresLoadedOnce = ref(false);

	const categoriasError = ref("");
	const proveedoresError = ref("");

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

	onMounted(async () => {
		await Promise.all([loadProducts(), loadCategorias(), loadProveedores()]);
	});

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
		const codigo = p?.codigo ?? p?.Codigo ?? "";
		const nombre = p?.nombre ?? p?.Nombre ?? "";
		const descripcion = p?.descripcion ?? p?.Descripcion ?? "";
		const idCategoria = p?.idCategoria ?? p?.IdCategoria ?? p?.categoriaId ?? p?.CategoriaId ?? 0;
		const idProveedor = p?.idProveedor ?? p?.IdProveedor ?? p?.proveedorId ?? p?.ProveedorId ?? 0;
		const precioCompra = p?.precioCompra ?? p?.PrecioCompra ?? 0;
		const precioVenta = p?.precioVenta ?? p?.PrecioVenta ?? 0;
		const stockActual = p?.stockActual ?? p?.StockActual ?? 0;
		const stockMinimo = p?.stockMinimo ?? p?.StockMinimo ?? 0;

		return {
			...p,
			idProducto: idProducto != null ? Number(idProducto) : null,
			codigo: String(codigo ?? "").trim(),
			nombre: String(nombre ?? "").trim(),
			descripcion: String(descripcion ?? "").trim(),
			idCategoria: Number(idCategoria ?? 0),
			idProveedor: Number(idProveedor ?? 0),
			precioCompra: Number(precioCompra ?? 0),
			precioVenta: Number(precioVenta ?? 0),
			stockActual: Number(stockActual ?? 0),
			stockMinimo: Number(stockMinimo ?? 0),
		};
	}

	function normalizeCategoria(x) {
		const idCategoria =
			x?.idCategoria ??
			x?.IdCategoria ??
			x?.categoriaId ??
			x?.CategoriaId ??
			x?.id ??
			x?.Id ??
			null;

		const nombre =
			x?.nombre ??
			x?.Nombre ??
			x?.descripcion ??
			x?.Descripcion ??
			x?.name ??
			"";

		return { ...x, idCategoria: idCategoria != null ? Number(idCategoria) : null, nombre: String(nombre ?? "").trim() };
	}

	function normalizeProveedor(x) {
		const idProveedor =
			x?.idProveedor ??
			x?.IdProveedor ??
			x?.proveedorId ??
			x?.ProveedorId ??
			x?.id ??
			x?.Id ??
			null;

		const nombreEmpresa =
			x?.nombreEmpresa ??
			x?.NombreEmpresa ??
			x?.nombre ??
			x?.Nombre ??
			x?.empresa ??
			x?.Empresa ??
			x?.name ??
			"";

		return { ...x, idProveedor: idProveedor != null ? Number(idProveedor) : null, nombreEmpresa: String(nombreEmpresa ?? "").trim() };
	}

	async function loadProducts() {
		loading.value = true;
		apiError.value = "";
		try {
			const data = await apiFetch("/api/Productos");
			rows.value = normalizeList(data).map(normalizeProducto);
		} catch (e) {
			rows.value = [];
			apiError.value = e?.message ?? "No se pudo cargar productos desde la API.";
		} finally {
			loading.value = false;
		}
	}

	function readAnyError(e) {
		const msg = String(e?.message ?? "");
		if (msg) return msg;
		return "Error en la API.";
	}

	async function fetchFirstList(paths) {
		let lastErr = null;

		for (const path of paths) {
			try {
				const data = await apiFetch(path);
				return { path, list: normalizeList(data) };
			} catch (e) {
				lastErr = e;
			}
		}
		throw (lastErr ?? new Error("No se pudo cargar la lista."));
	}

	async function loadCategorias() {
		categoriasLoading.value = true;
		categoriasLoadedOnce.value = true;
		categoriasError.value = "";

		try {
			const { list } = await fetchFirstList([
				"/api/Categorias",
				"/api/Categoria",
				"/api/Categoría",
				"/api/Categories",
			]);
			categorias.value = list.map(normalizeCategoria).filter((x) => x.idCategoria != null && x.nombre);
		} catch (e) {
			categorias.value = [];
			categoriasError.value = readAnyError(e);
		} finally {
			categoriasLoading.value = false;
		}
	}

	async function loadProveedores() {
		proveedoresLoading.value = true;
		proveedoresLoadedOnce.value = true;
		proveedoresError.value = "";

		try {
			const { list } = await fetchFirstList([
				"/api/Proveedores",
				"/api/Proveedor",
				"/api/Suplidores",
				"/api/Suppliers",
			]);
			proveedores.value = list.map(normalizeProveedor).filter((x) => x.idProveedor != null && x.nombreEmpresa);
		} catch (e) {
			proveedores.value = [];
			proveedoresError.value = readAnyError(e);
		} finally {
			proveedoresLoading.value = false;
		}
	}

	function rowKey(p) {
		return String(p?.idProducto ?? p?.codigo ?? `${p?.nombre}-${Math.random()}`);
	}

	function categoriaNombre(p) {
		const nested = p?.categoria?.nombre ?? p?.categoria?.Nombre;
		if (nested) return nested;

		const id = p?.idCategoria ?? p?.IdCategoria ?? null;
		if (!id) return "";

		const found = categorias.value.find((c) => Number(c.idCategoria) === Number(id));
		return found?.nombre ?? `ID ${id}`;
	}

	function proveedorNombre(p) {
		const nested = p?.proveedor?.nombreEmpresa ?? p?.proveedor?.NombreEmpresa ?? p?.proveedor?.nombre ?? p?.proveedor?.Nombre;
		if (nested) return nested;

		const id = p?.idProveedor ?? p?.IdProveedor ?? null;
		if (!id) return "";

		const found = proveedores.value.find((x) => Number(x.idProveedor) === Number(id));
		return found?.nombreEmpresa ?? `ID ${id}`;
	}

	const filteredRows = computed(() => {
		const q = search.value.trim().toLowerCase();
		if (!q) return rows.value;

		return rows.value.filter((p) => {
			const cat = String(categoriaNombre(p) ?? "").toLowerCase();
			const prov = String(proveedorNombre(p) ?? "").toLowerCase();

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

	async function ensureCombosLoaded() {
		if (!categoriasLoadedOnce.value) await loadCategorias();
		if (!proveedoresLoadedOnce.value) await loadProveedores();
	}

	async function openCreate() {
		if (!canEdit.value) return;

		apiError.value = "";
		mode.value = "create";
		editingIdProducto.value = null;

		Object.assign(form, emptyForm());

		await ensureCombosLoaded();
		if (categorias.value.length) form.idCategoria = Number(categorias.value[0].idCategoria);
		if (proveedores.value.length) form.idProveedor = Number(proveedores.value[0].idProveedor);

		isOpen.value = true;
	}

	async function openEdit(p) {
		if (!canEdit.value) return;

		apiError.value = "";

		const id = p?.idProducto ?? null;
		if (!id) {
			apiError.value = "Este registro no tiene idProducto.";
			return;
		}

		mode.value = "edit";
		editingIdProducto.value = Number(id);

		await ensureCombosLoaded();

		Object.assign(form, emptyForm(), {
			idProducto: Number(id),
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
		if (Number(form.idCategoria) <= 0) return "Debes seleccionar una Categoría.";
		if (Number(form.idProveedor) <= 0) return "Debes seleccionar un Proveedor.";
		if (Number(form.precioCompra) < 0 || Number(form.precioVenta) < 0) return "Los precios no pueden ser negativos.";
		if (Number(form.stockActual) < 0 || Number(form.stockMinimo) < 0) return "El stock no puede ser negativo.";
		if (Number(form.precioVenta) < Number(form.precioCompra)) return "El Precio de Venta no puede ser menor que el Precio de Compra.";
		return "";
	}

	async function saveProduct() {
		if (!canEdit.value) return;

		apiError.value = "";
		const err = validate();
		if (err) { apiError.value = err; return; }

		saving.value = true;
		try {
			const payload = {
				codigo: String(form.codigo ?? "").trim(),
				nombre: String(form.nombre ?? "").trim(),
				descripcion: String(form.descripcion ?? "").trim(),
				idCategoria: Number(form.idCategoria),
				idProveedor: Number(form.idProveedor),
				precioCompra: Number(form.precioCompra),
				precioVenta: Number(form.precioVenta),
				stockActual: Number(form.stockActual),
				stockMinimo: Number(form.stockMinimo),
			};

			if (mode.value === "create") {
				const created = await apiFetch("/api/Productos", {
					method: "POST",
					body: JSON.stringify(payload),
				});

				if (created) rows.value.unshift(normalizeProducto(created));
				else await loadProducts();

				closeModal();
				return;
			}

			if (!editingIdProducto.value) {
				apiError.value = "No hay idProducto para editar.";
				return;
			}

			await apiFetch(`/api/Productos/${encodeURIComponent(editingIdProducto.value)}`, {
				method: "PUT",
				body: JSON.stringify(payload),
			});

			await loadProducts();
			closeModal();
		} catch (e) {
			apiError.value = e?.message ?? "Error guardando el producto.";
		} finally {
			saving.value = false;
		}
	}

	async function removeProduct(p) {
		if (!canEdit.value) return;

		const id = p?.idProducto ?? null;
		const name = p?.nombre ?? p?.codigo ?? "este producto";

		if (!id) {
			alert("Este registro no tiene idProducto. No se puede eliminar.");
			return;
		}

		if (!confirm(`¿Seguro que deseas eliminar ${name}?`)) return;

		try {
			await apiFetch(`/api/Productos/${encodeURIComponent(id)}`, { method: "DELETE" });
			rows.value = rows.value.filter((r) => Number(r?.idProducto) !== Number(id));
		} catch (e) {
			alert(e?.message ?? "No se pudo eliminar.");
		}
	}
</script>

<style scoped>
	/* TU CSS SE QUEDA IGUAL - NO CAMBIÉ NADA AQUÍ */
	.page {
		min-height: 100vh;
		background: #eef3ff;
	}

	.content {
		padding: 22px;
	}

	.miniWarn {
		margin-top: 8px;
		color: #b45309;
		font-weight: 800;
		font-size: 12px;
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

		.field input, .field textarea, .field select {
			width: 100%;
			box-sizing: border-box;
			border: 1px solid rgba(148,163,184,.55);
			border-radius: 10px;
			padding: 12px 14px;
			font-size: 14px;
			outline: none;
			background: #fff;
			transition: border-color .15s ease, box-shadow .15s ease;
			pointer-events: auto;
		}

		.field select {
			appearance: none;
			background-image: linear-gradient(45deg, transparent 50%, #64748b 50%), linear-gradient(135deg, #64748b 50%, transparent 50%);
			background-position: calc(100% - 18px) calc(50% + 1px), calc(100% - 12px) calc(50% + 1px);
			background-size: 6px 6px, 6px 6px;
			background-repeat: no-repeat;
		}

		.field textarea {
			resize: vertical;
			min-height: 110px;
		}

			.field input:focus, .field textarea:focus, .field select:focus {
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

	.stockRow {
		display: flex;
		gap: 22px;
		position: relative;
		z-index: 50;
		isolation: isolate;
	}

		.stockRow .half {
			flex: 1;
			min-width: 0;
			position: relative;
			z-index: 60;
		}

		.stockRow input {
			position: relative;
			z-index: 70;
			pointer-events: auto;
		}

	@media (max-width: 980px) {
		.grid2 {
			grid-template-columns: 1fr;
		}

		.stockRow {
			flex-direction: column;
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