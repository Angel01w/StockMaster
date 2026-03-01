<template>
	<div class="page">
		<div class="content">
			<div class="hdr">
				<div class="hdr-left">
					<div class="cube">
						<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
							<path d="M4 7h16M7 7v14m10-14v14M6 21h12" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
							<path d="M9 7V5a3 3 0 0 1 6 0v2" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
						</svg>
					</div>
					<div class="h1">Gestión de Proveedores</div>
				</div>

				<button v-if="canEdit" class="btn-primary" type="button" @click="openCreate">
					<span class="plus">＋</span>
					Nuevo Proveedor
				</button>
			</div>

			<div v-if="loadError" class="alert">
				{{ loadError }}
			</div>

			<div class="search">
				<div class="search-ic">
					<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
						<path d="M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15Z" stroke="currentColor" stroke-width="1.8" />
						<path d="M16.5 16.5 21 21" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
					</svg>
				</div>
				<input v-model="searchQ" class="search-in" placeholder="Buscar proveedores..." />
			</div>

			<div v-if="loading" class="mutedLine">Cargando proveedores...</div>

			<div v-else-if="filteredProviders.length === 0" class="mutedLine">
				No hay proveedores para mostrar.
			</div>

			<div v-else class="grid">
				<div class="card" v-for="p in filteredProviders" :key="rowKey(p)">
					<div class="card-top">
						<div class="tag-ico">
							<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
								<path d="M4 7h9l7 7-7 7-9-9V7Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
								<path d="M7.5 10.5h.01" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
							</svg>
						</div>

						<div class="actions">
							<button v-if="canEdit"
									class="icon-btn edit"
									type="button"
									title="Editar"
									aria-label="Editar"
									@click="openEdit(p)">
								✎
							</button>
							<button v-if="canEdit"
									class="icon-btn del"
									type="button"
									title="Eliminar"
									aria-label="Eliminar"
									@click="removeProvider(p)">
								🗑
							</button>

							<span v-if="!canEdit" class="mutedBadge">Solo lectura</span>
						</div>
					</div>

					<div class="title">{{ p.nombreEmpresa }}</div>
					<div class="sub">{{ p.personaContacto }}</div>

					<div class="info">
						<div class="info-row">
							<span class="i">✉</span>
							<span class="t">{{ p.email }}</span>
						</div>
						<div class="info-row">
							<span class="i">📞</span>
							<span class="t">{{ p.telefono }}</span>
						</div>
						<div class="info-row">
							<span class="i">📍</span>
							<span class="t">{{ p.direccion }}</span>
						</div>
					</div>

					<div class="card-foot">
						<div class="foot-lbl">Productos Suministrados</div>
						<div class="foot-num">{{ Number(p.productosSuministrados ?? 0) }}</div>
					</div>
				</div>
			</div>

			<div v-if="isOpen && canEdit" class="overlay" @click.self="closeModal">
				<div class="drawer" role="dialog" aria-modal="true">
					<div class="drawer-head">
						<div class="drawer-title">
							{{ mode === "create" ? "Nuevo Proveedor" : "Editar Proveedor" }}
						</div>
						<button class="xBtn" type="button" @click="closeModal" aria-label="Cerrar">×</button>
					</div>

					<div class="drawer-body">
						<div v-if="apiError" class="alert">{{ apiError }}</div>

						<div class="grid2">
							<div class="field">
								<label>Nombre de la Empresa</label>
								<input v-model.trim="form.nombreEmpresa" autocomplete="off" :disabled="!canEdit" />
							</div>

							<div class="field">
								<label>Persona de Contacto</label>
								<input v-model.trim="form.personaContacto" autocomplete="off" :disabled="!canEdit" />
							</div>
						</div>

						<div class="grid2">
							<div class="field">
								<label>Email</label>
								<input v-model.trim="form.email" type="email" autocomplete="off" :disabled="!canEdit" />
							</div>

							<div class="field">
								<label>Teléfono</label>
								<input v-model.trim="form.telefono" autocomplete="off" :disabled="!canEdit" />
							</div>
						</div>

						<div class="field">
							<label>Dirección</label>
							<textarea v-model.trim="form.direccion" rows="6" :disabled="!canEdit"></textarea>
						</div>
					</div>

					<div class="drawer-foot">
						<button class="btnLink" type="button" @click="closeModal">Cancelar</button>
						<button class="btnPrimary" type="button" :disabled="saving || !canEdit" @click="saveProvider">
							{{ saving ? (mode === "create" ? "Creando..." : "Guardando...") : (mode === "create" ? "Crear Proveedor" : "Guardar Cambios") }}
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
	const canEdit = computed(() => perms.value?.canEditProveedores === true || perms.value?.canEditAll === true);

	const PROVIDERS_ENDPOINT = "/api/Proveedores";

	const searchQ = ref("");
	const loading = ref(false);
	const loadError = ref("");

	const isOpen = ref(false);
	const saving = ref(false);
	const apiError = ref("");

	const mode = ref("create");
	const editingIdProveedor = ref(null);

	const providers = ref([]);

	const emptyForm = () => ({
		nombreEmpresa: "",
		personaContacto: "",
		email: "",
		telefono: "",
		direccion: "",
	});
	const form = reactive(emptyForm());

	onMounted(loadProviders);

	function rowKey(p) {
		return String(p?.idProveedor ?? p?.IdProveedor ?? p?.nombreEmpresa ?? `${Math.random()}`);
	}

	function normalizeProvider(p) {
		const idProveedor = p?.idProveedor ?? p?.IdProveedor ?? p?.id ?? p?.Id ?? null;

		const nombreEmpresa =
			p?.nombreEmpresa ??
			p?.NombreEmpresa ??
			p?.nombre ??
			p?.Nombre ??
			p?.empresa ??
			p?.Empresa ??
			"";

		const personaContacto =
			p?.personaContacto ??
			p?.PersonaContacto ??
			p?.contacto ??
			p?.Contacto ??
			"";

		const email = p?.email ?? p?.Email ?? "";
		const telefono = p?.telefono ?? p?.Telefono ?? "";
		const direccion = p?.direccion ?? p?.Direccion ?? "";

		const productosSuministrados =
			p?.productosSuministrados ??
			p?.ProductosSuministrados ??
			p?.totalProductos ??
			p?.TotalProductos ??
			0;

		return {
			...p,
			idProveedor: idProveedor != null ? Number(idProveedor) : null,
			nombreEmpresa: String(nombreEmpresa ?? "").trim(),
			personaContacto: String(personaContacto ?? "").trim(),
			email: String(email ?? "").trim(),
			telefono: String(telefono ?? "").trim(),
			direccion: String(direccion ?? "").trim(),
			productosSuministrados: Number(productosSuministrados ?? 0),
		};
	}

	function normalizeList(data) {
		if (Array.isArray(data)) return data.map(normalizeProvider);
		if (Array.isArray(data?.$values)) return data.$values.map(normalizeProvider);
		if (Array.isArray(data?.items)) return data.items.map(normalizeProvider);
		if (Array.isArray(data?.data)) return data.data.map(normalizeProvider);
		if (Array.isArray(data?.result)) return data.result.map(normalizeProvider);
		if (Array.isArray(data?.value)) return data.value.map(normalizeProvider);
		if (Array.isArray(data?.results)) return data.results.map(normalizeProvider);
		return [];
	}

	async function loadProviders() {
		loading.value = true;
		loadError.value = "";
		try {
			const data = await apiFetch(PROVIDERS_ENDPOINT);
			providers.value = normalizeList(data);
		} catch (e) {
			loadError.value = e?.message ?? "Failed to fetch.";
			providers.value = [];
		} finally {
			loading.value = false;
		}
	}

	const filteredProviders = computed(() => {
		const q = searchQ.value.trim().toLowerCase();
		if (!q) return providers.value;

		return providers.value.filter((p) => {
			return (
				String(p.nombreEmpresa ?? "").toLowerCase().includes(q) ||
				String(p.personaContacto ?? "").toLowerCase().includes(q) ||
				String(p.email ?? "").toLowerCase().includes(q) ||
				String(p.telefono ?? "").toLowerCase().includes(q) ||
				String(p.direccion ?? "").toLowerCase().includes(q)
			);
		});
	});

	function openCreate() {
		if (!canEdit.value) return;

		apiError.value = "";
		mode.value = "create";
		editingIdProveedor.value = null;
		Object.assign(form, emptyForm());
		isOpen.value = true;
	}

	function openEdit(p) {
		if (!canEdit.value) return;

		apiError.value = "";
		mode.value = "edit";

		const id = p?.idProveedor ?? p?.IdProveedor ?? null;
		if (!id) {
			apiError.value = "Este registro no tiene 'idProveedor'.";
			editingIdProveedor.value = null;
			Object.assign(form, emptyForm(), normalizeProvider(p));
			isOpen.value = true;
			return;
		}

		editingIdProveedor.value = Number(id);
		Object.assign(form, emptyForm(), {
			nombreEmpresa: p?.nombreEmpresa ?? p?.NombreEmpresa ?? "",
			personaContacto: p?.personaContacto ?? p?.PersonaContacto ?? "",
			email: p?.email ?? p?.Email ?? "",
			telefono: p?.telefono ?? p?.Telefono ?? "",
			direccion: p?.direccion ?? p?.Direccion ?? "",
		});
		isOpen.value = true;
	}

	function closeModal() {
		isOpen.value = false;
	}

	function validate() {
		if (!form.nombreEmpresa) return "El Nombre de la Empresa es obligatorio.";
		if (!form.personaContacto) return "La Persona de Contacto es obligatoria.";
		if (!form.email) return "El Email es obligatorio.";
		return "";
	}

	async function saveProvider() {
		if (!canEdit.value) return;

		apiError.value = "";
		const err = validate();
		if (err) { apiError.value = err; return; }

		saving.value = true;
		try {
			const payload = {
				nombreEmpresa: String(form.nombreEmpresa ?? "").trim(),
				personaContacto: String(form.personaContacto ?? "").trim(),
				email: String(form.email ?? "").trim(),
				telefono: String(form.telefono ?? "").trim(),
				direccion: String(form.direccion ?? "").trim(),
			};

			if (mode.value === "create") {
				await apiFetch(PROVIDERS_ENDPOINT, { method: "POST", body: JSON.stringify(payload) });
				await loadProviders();
				closeModal();
				return;
			}

			if (!editingIdProveedor.value) {
				apiError.value = "No hay idProveedor para editar.";
				return;
			}

			const url = `${PROVIDERS_ENDPOINT}/${encodeURIComponent(editingIdProveedor.value)}`;
			await apiFetch(url, { method: "PUT", body: JSON.stringify(payload) });
			await loadProviders();
			closeModal();
		} catch (e) {
			apiError.value = e?.message ?? "Error guardando el proveedor.";
		} finally {
			saving.value = false;
		}
	}

	async function removeProvider(p) {
		if (!canEdit.value) return;

		const id = p?.idProveedor ?? p?.IdProveedor ?? null;
		const name = p?.nombreEmpresa ?? p?.NombreEmpresa ?? "este proveedor";

		if (!id) {
			alert("Este registro no tiene 'idProveedor'.");
			return;
		}

		if (!confirm(`¿Seguro que deseas eliminar ${name}?`)) return;

		try {
			const url = `${PROVIDERS_ENDPOINT}/${encodeURIComponent(id)}`;
			await apiFetch(url, { method: "DELETE" });
			providers.value = providers.value.filter((x) => Number(x?.idProveedor) !== Number(id));
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
		padding: 18px 22px 28px;
	}

	.hdr {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 2px;
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
		font-size: 28px;
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

	.search {
		height: 48px;
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 0 14px;
		border-radius: 14px;
		background: rgba(255,255,255,.92);
		border: 1px solid rgba(15,23,42,.08);
		box-shadow: 0 10px 22px rgba(10,20,70,.06);
		margin-bottom: 16px;
	}

	.search-ic {
		width: 18px;
		height: 18px;
		opacity: .7;
		color: #64748b;
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

	.mutedLine {
		color: #64748b;
		font-weight: 800;
		padding: 10px 2px;
	}

	.mutedBadge {
		display: inline-flex;
		align-items: center;
		height: 30px;
		padding: 0 10px;
		border-radius: 999px;
		font-weight: 900;
		font-size: 12px;
		color: #64748b;
		background: rgba(148,163,184,.16);
		border: 1px solid rgba(148,163,184,.22);
	}

	.grid {
		display: grid;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		gap: 16px;
	}

	.card {
		background: rgba(255,255,255,.92);
		border: 1px solid rgba(15,23,42,.08);
		border-radius: 14px;
		box-shadow: 0 12px 24px rgba(10,20,70,.06);
		padding: 16px 16px 12px;
	}

	.card-top {
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		margin-bottom: 10px;
	}

	.tag-ico {
		width: 44px;
		height: 44px;
		border-radius: 12px;
		display: grid;
		place-items: center;
		background: rgba(59,130,246,.12);
		color: #2563eb;
	}

		.tag-ico svg {
			width: 22px;
			height: 22px;
		}

	.actions {
		display: flex;
		gap: 10px;
		align-items: center;
	}

	.icon-btn {
		width: 32px;
		height: 32px;
		border-radius: 10px;
		border: 0;
		background: transparent;
		cursor: pointer;
		font-size: 15px;
		opacity: .95;
	}

		.icon-btn.edit {
			color: #2563eb;
		}

		.icon-btn.del {
			color: #ef4444;
		}

	.title {
		font-weight: 900;
		color: #0f172a;
		font-size: 16px;
	}

	.sub {
		margin-top: 2px;
		font-weight: 700;
		color: #64748b;
		font-size: 13px;
	}

	.info {
		margin-top: 12px;
		display: flex;
		flex-direction: column;
		gap: 8px;
		color: #64748b;
		font-weight: 700;
		font-size: 13px;
	}

	.info-row {
		display: flex;
		align-items: center;
		gap: 10px;
	}

		.info-row .i {
			width: 18px;
			opacity: .8;
		}

		.info-row .t {
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
		}

	.card-foot {
		margin-top: 14px;
		padding-top: 12px;
		border-top: 1px solid rgba(15,23,42,.06);
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.foot-lbl {
		color: #94a3b8;
		font-weight: 800;
		font-size: 12px;
	}

	.foot-num {
		color: #2563eb;
		font-weight: 900;
	}

	.overlay {
		position: fixed;
		inset: 0;
		background: rgba(15,23,42,.25);
		z-index: 2000;
	}

	.drawer {
		position: absolute;
		right: 0;
		top: 0;
		height: 100vh;
		width: 520px;
		max-width: calc(100vw - 36px);
		background: #fff;
		border-left: 1px solid rgba(15,23,42,.10);
		box-shadow: -18px 0 40px rgba(0,0,0,.14);
		display: flex;
		flex-direction: column;
	}

	.drawer-head {
		height: 70px;
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0 18px;
		border-bottom: 1px solid rgba(15,23,42,.10);
	}

	.drawer-title {
		font-weight: 600;
		font-size: 34px;
		color: #0f172a;
		letter-spacing: -.5px;
	}

	.xBtn {
		width: 44px;
		height: 44px;
		border-radius: 10px;
		border: 0;
		background: transparent;
		cursor: pointer;
		font-size: 26px;
		line-height: 1;
		color: #0f172a;
	}

	.drawer-body {
		padding: 18px;
		display: flex;
		flex-direction: column;
		gap: 16px;
		overflow: auto;
	}

	.grid2 {
		display: grid;
		grid-template-columns: minmax(0,1fr) minmax(0,1fr);
		gap: 16px;
	}

	.field label {
		display: block;
		margin-bottom: 10px;
		font-weight: 800;
		color: #64748b;
		font-size: 18px;
	}

	.field input, .field textarea {
		width: 100%;
		box-sizing: border-box;
		border: 1px solid rgba(148,163,184,.55);
		border-radius: 10px;
		padding: 14px 14px;
		font-size: 14px;
		outline: none;
		background: #fff;
	}

	.field textarea {
		resize: vertical;
		min-height: 160px;
	}

		.field input:focus, .field textarea:focus {
			border-color: rgba(59,130,246,.65);
			box-shadow: 0 0 0 3px rgba(59,130,246,.18);
		}

	.drawer-foot {
		padding: 14px 18px 18px;
		display: flex;
		justify-content: flex-end;
		align-items: center;
		gap: 18px;
		border-top: 1px solid rgba(15,23,42,.06);
	}

	.btnLink {
		border: 0;
		background: transparent;
		color: #64748b;
		font-weight: 900;
		cursor: pointer;
		padding: 10px 14px;
		font-size: 18px;
	}

	.btnPrimary {
		border: 0;
		cursor: pointer;
		padding: 14px 20px;
		border-radius: 14px;
		color: #fff;
		font-weight: 900;
		background: linear-gradient(180deg,#2f74ff,#1e5ae9);
		box-shadow: 0 14px 28px rgba(37,99,235,.25);
		font-size: 18px;
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

	@media (max-width: 1100px) {
		.grid {
			grid-template-columns: 1fr;
		}

		.grid2 {
			grid-template-columns: 1fr;
		}
	}
</style>