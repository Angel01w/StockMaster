<template>
	<div class="page">
		<div class="content">
			<div class="hdr">
				<div class="hdr-left">
					<div class="cube">
						<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
							<path d="M20 13.5V7a2 2 0 0 0-2-2h-6.5a2 2 0 0 0-1.4.6L4.6 11.1a2 2 0 0 0 0 2.8l5.5 5.5a2 2 0 0 0 2.8 0l5.5-5.5a2 2 0 0 0 .6-1.4Z"
								  stroke="currentColor"
								  stroke-width="1.8"
								  stroke-linejoin="round" />
							<path d="M15.5 9.5h.01" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
						</svg>
					</div>
					<div>
						<div class="h1">Gestión de Categorías</div>
					</div>
				</div>

				<button v-if="canEdit" class="btn-primary" type="button" @click="openCreate">
					<span class="plus">＋</span>
					Nueva Categoría
				</button>
			</div>

			<div class="search">
				<div class="search-ic">🔍</div>
				<input v-model="searchQ" class="search-in" placeholder="Buscar categorías..." />
			</div>

			<div v-if="loadError" class="alert" style="margin-bottom: 16px">
				{{ loadError }}
			</div>

			<div v-if="apiError && !isOpen" class="alert" style="margin-bottom: 16px">
				{{ apiError }}
			</div>

			<div class="grid">
				<div v-if="!loading && filteredRows.length === 0" class="empty">
					No hay categorías. Crea una con “Nueva Categoría”.
				</div>

				<div class="cat-card" v-for="c in filteredRows" :key="rowKey(c)">
					<div class="card-top">
						<div class="tag-badge" aria-hidden="true">
							<svg viewBox="0 0 24 24" fill="none">
								<path d="M20 13.5V7a2 2 0 0 0-2-2h-6.5a2 2 0 0 0-1.4.6L4.6 11.1a2 2 0 0 0 0 2.8l5.5 5.5a2 2 0 0 0 2.8 0l5.5-5.5a2 2 0 0 0 .6-1.4Z"
									  stroke="currentColor"
									  stroke-width="1.8"
									  stroke-linejoin="round" />
								<path d="M15.5 9.5h.01" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
							</svg>
						</div>

						<div class="card-actions" v-if="canEdit">
							<button class="aicon edit" type="button" title="Editar" aria-label="Editar" @click="openEdit(c)">✎</button>
							<button class="aicon del" type="button" title="Eliminar" aria-label="Eliminar" @click="removeCategory(c)">🗑</button>
						</div>
					</div>

					<div class="cat-name">{{ c.nombre }}</div>
					<div class="cat-desc">{{ c.descripcion }}</div>

					<div class="card-foot">
						<div class="foot-lbl">Productos</div>
						<div class="foot-num">{{ productosCount(c) }}</div>
					</div>
				</div>
			</div>

			<div v-if="isOpen" class="drawerOverlay" @click.self="closeModal">
				<div class="drawer" role="dialog" aria-modal="true">
					<div class="drawerHead">
						<div class="drawerTitle">
							{{ mode === "create" ? "Nueva Categoría" : "Editar Categoría" }}
						</div>
						<button class="xBtn" type="button" @click="closeModal" aria-label="Cerrar">×</button>
					</div>

					<div class="drawerBody">
						<div v-if="apiError" class="alert">{{ apiError }}</div>

						<div class="field">
							<label>Nombre</label>
							<input v-model.trim="form.nombre" autocomplete="off" :disabled="!canEdit" />
						</div>

						<div class="field">
							<label>Descripción</label>
							<textarea v-model.trim="form.descripcion" rows="10" :disabled="!canEdit"></textarea>
						</div>
					</div>

					<div class="drawerFoot">
						<button class="btnLink" type="button" @click="closeModal">Cancelar</button>

						<button v-if="canEdit" class="btnPrimary" type="button" :disabled="saving" @click="saveCategory">
							{{
								saving
									? mode === "create"
										? "Creando..."
										: "Guardando..."
									: mode === "create"
									? "Crear Categoría"
									: "Guardar Cambios"
							}}
						</button>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup>
	import { computed, onMounted, reactive, ref } from "vue";
	import { getUser, getPermsSafe } from "../router/auth.service";
	import { apiFetch } from "../services/api";

	const user = computed(() => getUser());
	const perms = computed(() => getPermsSafe());
	const canEdit = computed(() => perms.value?.canEditCategorias === true || perms.value?.canEditAll === true);

	const CATS_ENDPOINT = "/api/Categorias";

	const searchQ = ref("");
	const loading = ref(false);
	const loadError = ref("");

	const isOpen = ref(false);
	const saving = ref(false);
	const apiError = ref("");

	const mode = ref("create");
	const editingIdCategoria = ref(null);

	const rows = ref([]);

	const emptyForm = () => ({ nombre: "", descripcion: "" });
	const form = reactive(emptyForm());

	onMounted(loadCategorias);

	function rowKey(c) {
		return String(c?.idCategoria ?? c?.IdCategoria ?? c?.nombre ?? `${Math.random()}`);
	}

	function normalizeCat(c) {
		const idCategoria = c?.idCategoria ?? c?.IdCategoria ?? c?.id ?? c?.Id ?? null;
		const nombre = c?.nombre ?? c?.Nombre ?? "";
		const descripcion = c?.descripcion ?? c?.Descripcion ?? null;

		return {
			...c,
			idCategoria: idCategoria != null ? Number(idCategoria) : null,
			nombre: String(nombre ?? "").trim(),
			descripcion: descripcion == null ? "" : String(descripcion).trim(),
		};
	}

	function normalizeList(data) {
		if (Array.isArray(data)) return data.map(normalizeCat);
		if (Array.isArray(data?.$values)) return data.$values.map(normalizeCat);
		if (Array.isArray(data?.items)) return data.items.map(normalizeCat);
		if (Array.isArray(data?.data)) return data.data.map(normalizeCat);
		if (Array.isArray(data?.result)) return data.result.map(normalizeCat);
		if (Array.isArray(data?.value)) return data.value.map(normalizeCat);
		if (Array.isArray(data?.results)) return data.results.map(normalizeCat);
		return [];
	}

	async function loadCategorias() {
		loading.value = true;
		loadError.value = "";
		try {
			const data = await apiFetch(CATS_ENDPOINT);
			rows.value = normalizeList(data);
		} catch (e) {
			loadError.value = e?.message ?? "Failed to fetch.";
			rows.value = [];
		} finally {
			loading.value = false;
		}
	}

	const filteredRows = computed(() => {
		const q = searchQ.value.trim().toLowerCase();
		if (!q) return rows.value;
		return rows.value.filter((c) => {
			return (
				String(c.nombre ?? "").toLowerCase().includes(q) ||
				String(c.descripcion ?? "").toLowerCase().includes(q)
			);
		});
	});

	function productosCount(c) {
		return Number(c?.productos ?? c?.Productos ?? c?.productosCount ?? c?.ProductosCount ?? 0);
	}

	function openCreate() {
		if (!canEdit.value) return;
		apiError.value = "";
		mode.value = "create";
		editingIdCategoria.value = null;
		Object.assign(form, emptyForm());
		isOpen.value = true;
	}

	function openEdit(c) {
		if (!canEdit.value) return;
		apiError.value = "";
		mode.value = "edit";

		const id = c?.idCategoria ?? c?.IdCategoria ?? null;
		if (!id) {
			apiError.value = "Este registro no tiene 'idCategoria'.";
			editingIdCategoria.value = null;
			Object.assign(form, emptyForm(), normalizeCat(c));
			isOpen.value = true;
			return;
		}

		editingIdCategoria.value = Number(id);
		Object.assign(form, emptyForm(), {
			nombre: c?.nombre ?? c?.Nombre ?? "",
			descripcion: c?.descripcion ?? c?.Descripcion ?? "",
		});
		isOpen.value = true;
	}

	function closeModal() {
		isOpen.value = false;
	}

	function validate() {
		if (!form.nombre) return "El Nombre es obligatorio.";
		return "";
	}

	async function saveCategory() {
		if (!canEdit.value) return;

		apiError.value = "";
		const err = validate();
		if (err) { apiError.value = err; return; }

		saving.value = true;
		try {
			const payload = {
				nombre: String(form.nombre ?? "").trim(),
				descripcion: String(form.descripcion ?? "").trim() || null,
			};

			if (mode.value === "create") {
				await apiFetch(CATS_ENDPOINT, { method: "POST", body: JSON.stringify(payload) });
				await loadCategorias();
				closeModal();
				return;
			}

			if (!editingIdCategoria.value) {
				apiError.value = "No hay idCategoria para editar.";
				return;
			}

			const url = `${CATS_ENDPOINT}/${encodeURIComponent(editingIdCategoria.value)}`;
			await apiFetch(url, { method: "PUT", body: JSON.stringify(payload) });
			await loadCategorias();
			closeModal();
		} catch (e) {
			apiError.value = e?.message ?? "Error guardando la categoría.";
		} finally {
			saving.value = false;
		}
	}

	async function removeCategory(c) {
		if (!canEdit.value) return;

		const id = c?.idCategoria ?? c?.IdCategoria ?? null;
		const name = c?.nombre ?? c?.Nombre ?? "esta categoría";

		if (!id) return;
		if (!confirm(`¿Seguro que deseas eliminar ${name}?`)) return;

		try {
			const url = `${CATS_ENDPOINT}/${encodeURIComponent(id)}`;
			await apiFetch(url, { method: "DELETE" });
			rows.value = rows.value.filter((x) => Number(x?.idCategoria) !== Number(id));
		} catch (e) {
			alert(e?.message ?? "No se pudo eliminar.");
		}
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
		font-size: 22px;
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
		height: 44px;
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 0 14px;
		border-radius: 12px;
		background: rgba(255, 255, 255, 0.92);
		border: 1px solid rgba(15, 23, 42, 0.08);
		box-shadow: 0 10px 22px rgba(10, 20, 70, 0.06);
		margin-bottom: 16px;
	}

	.search-ic {
		opacity: 0.75;
	}

	.search-in {
		border: 0;
		outline: none;
		width: 100%;
		font-size: 13px;
		background: transparent;
		color: #0f172a;
	}

	.grid {
		display: grid;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		gap: 18px;
		align-items: start;
	}

	.empty {
		grid-column: 1 / -1;
		padding: 14px 12px;
		color: #64748b;
		font-weight: 800;
	}

	.cat-card {
		background: rgba(255, 255, 255, 0.92);
		border: 1px solid rgba(15, 23, 42, 0.08);
		border-radius: 16px;
		box-shadow: 0 16px 30px rgba(10, 20, 70, 0.08);
		padding: 16px 16px 12px;
		min-height: 148px;
		display: flex;
		flex-direction: column;
	}

	.card-top {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		margin-bottom: 10px;
	}

	.tag-badge {
		width: 40px;
		height: 40px;
		border-radius: 12px;
		background: rgba(59, 130, 246, 0.10);
		display: grid;
		place-items: center;
		color: #2563eb;
	}

		.tag-badge svg {
			width: 20px;
			height: 20px;
		}

	.card-actions {
		display: flex;
		gap: 10px;
	}

	.aicon {
		width: 30px;
		height: 30px;
		border-radius: 10px;
		border: 1px solid rgba(15, 23, 42, 0.08);
		background: transparent;
		cursor: pointer;
		display: grid;
		place-items: center;
		box-shadow: 0 10px 18px rgba(10, 20, 70, 0.03);
		font-size: 14px;
	}

		.aicon.edit {
			color: #2563eb;
		}

		.aicon.del {
			color: #ef4444;
		}

	.cat-name {
		font-weight: 900;
		color: #0f172a;
		font-size: 14px;
		margin-top: 6px;
	}

	.cat-desc {
		margin-top: 6px;
		color: #94a3b8;
		font-weight: 700;
		font-size: 12px;
		line-height: 1.35;
		min-height: 32px;
	}

	.card-foot {
		margin-top: auto;
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding-top: 12px;
		border-top: 1px solid rgba(15, 23, 42, 0.06);
	}

	.foot-lbl {
		color: #94a3b8;
		font-weight: 800;
		font-size: 12px;
	}

	.foot-num {
		color: #2563eb;
		font-weight: 900;
		font-size: 14px;
	}

	.drawerOverlay {
		position: fixed;
		inset: 0;
		background: rgba(15, 23, 42, 0.12);
		z-index: 3000;
		display: flex;
		justify-content: flex-end;
	}

	.drawer {
		width: 520px;
		max-width: 88vw;
		height: 100vh;
		background: #fff;
		border-left: 1px solid rgba(15, 23, 42, 0.10);
		box-shadow: -18px 0 40px rgba(0, 0, 0, 0.08);
		display: flex;
		flex-direction: column;
	}

	.drawerHead {
		height: 72px;
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0 18px;
		border-bottom: 1px solid rgba(15, 23, 42, 0.10);
	}

	.drawerTitle {
		font-weight: 900;
		font-size: 28px;
		color: #0f172a;
	}

	.xBtn {
		width: 44px;
		height: 44px;
		border-radius: 12px;
		border: 0;
		background: transparent;
		cursor: pointer;
		font-size: 24px;
		line-height: 1;
		color: #0f172a;
	}

	.drawerBody {
		padding: 18px;
		display: flex;
		flex-direction: column;
		gap: 16px;
	}

	.field label {
		display: block;
		margin-bottom: 10px;
		font-weight: 900;
		color: #64748b;
		font-size: 14px;
	}

	.field input,
	.field textarea {
		width: 100%;
		box-sizing: border-box;
		border: 1px solid rgba(148, 163, 184, 0.55);
		border-radius: 12px;
		padding: 12px 14px;
		font-size: 14px;
		outline: none;
		background: #fff;
		transition: border-color 0.15s ease, box-shadow 0.15s ease;
	}

	.field textarea {
		resize: none;
		min-height: 220px;
	}

		.field input:focus,
		.field textarea:focus {
			border-color: rgba(59, 130, 246, 0.65);
			box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.18);
		}

	.drawerFoot {
		margin-top: auto;
		padding: 18px;
		display: flex;
		align-items: center;
		justify-content: flex-end;
		gap: 18px;
		border-top: 1px solid rgba(15, 23, 42, 0.10);
	}

	.btnLink {
		border: 0;
		background: transparent;
		color: #64748b;
		font-weight: 900;
		cursor: pointer;
		padding: 10px 14px;
		font-size: 15px;
	}

	.btnPrimary {
		border: 0;
		cursor: pointer;
		padding: 12px 18px;
		border-radius: 14px;
		color: #fff;
		font-weight: 900;
		background: linear-gradient(180deg, #2f74ff, #1e5ae9);
		box-shadow: 0 14px 28px rgba(37, 99, 235, 0.25);
		font-size: 15px;
	}

		.btnPrimary:disabled {
			opacity: 0.7;
			cursor: not-allowed;
		}

	.alert {
		border: 1px solid rgba(239, 68, 68, 0.25);
		background: rgba(239, 68, 68, 0.08);
		color: #b91c1c;
		padding: 10px 12px;
		border-radius: 12px;
		font-weight: 800;
	}

	@media (max-width: 1100px) {
		.grid {
			grid-template-columns: 1fr;
		}
	}
</style>