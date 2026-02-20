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

				<button class="btn-primary" type="button" @click="openCreate">
					<span class="plus">＋</span>
					Nuevo Proveedor
				</button>
			</div>

			<!-- Error global de carga -->
			<div v-if="loadError" class="alert">
				{{ loadError }}
			</div>

			<!-- Buscador -->
			<div class="search">
				<div class="search-ic">
					<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
						<path d="M10.5 18a7.5 7.5 0 1 1 0-15 7.5 7.5 0 0 1 0 15Z" stroke="currentColor" stroke-width="1.8" />
						<path d="M16.5 16.5 21 21" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
					</svg>
				</div>
				<input v-model="searchQ" class="search-in" placeholder="Buscar proveedores..." />
			</div>

			<!-- Loading -->
			<div v-if="loading" class="mutedLine">Cargando proveedores...</div>

			<!-- Empty -->
			<div v-else-if="filteredProviders.length === 0" class="mutedLine">
				No hay proveedores para mostrar.
			</div>

			<!-- Cards -->
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
							<button class="icon-btn edit" type="button" title="Editar" aria-label="Editar" @click="openEdit(p)">
								✎
							</button>
							<button class="icon-btn del" type="button" title="Eliminar" aria-label="Eliminar" @click="removeProvider(p)">
								🗑
							</button>
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
						<div class="foot-num">{{ p.productosSuministrados ?? 0 }}</div>
					</div>
				</div>
			</div>

			<!-- DRAWER -->
			<div v-if="isOpen" class="overlay" @click.self="closeModal">
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
								<input v-model.trim="form.nombreEmpresa" autocomplete="off" />
							</div>

							<div class="field">
								<label>Persona de Contacto</label>
								<input v-model.trim="form.personaContacto" autocomplete="off" />
							</div>
						</div>

						<div class="grid2">
							<div class="field">
								<label>Email</label>
								<input v-model.trim="form.email" type="email" autocomplete="off" />
							</div>

							<div class="field">
								<label>Teléfono</label>
								<input v-model.trim="form.telefono" autocomplete="off" />
							</div>
						</div>

						<div class="field">
							<label>Dirección</label>
							<textarea v-model.trim="form.direccion" rows="6"></textarea>
						</div>
					</div>

					<div class="drawer-foot">
						<button class="btnLink" type="button" @click="closeModal">Cancelar</button>
						<button class="btnPrimary" type="button" :disabled="saving" @click="saveProvider">
							{{ saving ? (mode === "create" ? "Creando..." : "Guardando...") : (mode === "create" ? "Crear Proveedor" : "Guardar Cambios") }}
						</button>
					</div>
				</div>
			</div>
			<!-- /DRAWER -->
		</div>
	</div>
</template>

<script setup>
	import { computed, onMounted, reactive, ref } from "vue";

	// ✅ AJUSTA si tu backend corre en otro puerto / dominio
	const API_BASE = "https://localhost:7198";

	// ✅ Según tu Swagger (imagen): /api/Proveedores
	const PROVIDERS_ENDPOINT = `${API_BASE}/api/Proveedores`;

	const searchQ = ref("");
	const loading = ref(false);
	const loadError = ref("");

	const isOpen = ref(false);
	const saving = ref(false);
	const apiError = ref("");

	const mode = ref("create"); // create | edit
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
		return String(p?.idProveedor ?? p?.nombreEmpresa ?? Math.random());
	}

	function normalizeProvider(p) {
		return {
			idProveedor: p?.idProveedor ?? null,
			nombreEmpresa: p?.nombreEmpresa ?? "",
			personaContacto: p?.personaContacto ?? "",
			email: p?.email ?? "",
			telefono: p?.telefono ?? "",
			direccion: p?.direccion ?? "",
			productosSuministrados: p?.productosSuministrados ?? p?.totalProductos ?? 0,
		};
	}

	function normalizeList(data) {
		const list = Array.isArray(data) ? data : (data?.items ?? []);
		return list.map(normalizeProvider);
	}

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
					JSON.stringify(data);
			} else {
				text = await res.text();
			}
		} catch { /* ignore */ }

		const msg = text?.trim()
			? `${res.status} ${res.statusText}: ${text}`
			: `${res.status} ${res.statusText}`;

		return new Error(msg);
	}

	async function loadProviders() {
		loading.value = true;
		loadError.value = "";
		try {
			const res = await fetch(PROVIDERS_ENDPOINT);
			if (!res.ok) throw await readApiError(res);

			const data = await res.json();
			providers.value = normalizeList(data);
		} catch (e) {
			// ✅ SIN FALLBACK: solo error
			loadError.value = e?.message ?? "Failed to fetch (revisa API_BASE, certificado HTTPS o CORS).";
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
		apiError.value = "";
		mode.value = "create";
		editingIdProveedor.value = null;
		Object.assign(form, emptyForm());
		isOpen.value = true;
	}

	function openEdit(p) {
		apiError.value = "";
		mode.value = "edit";

		const id = p?.idProveedor ?? null;
		if (!id) {
			apiError.value = "Este registro no tiene 'idProveedor'. La API debe devolver idProveedor para poder editar/eliminar.";
			editingIdProveedor.value = null;
			Object.assign(form, emptyForm(), normalizeProvider(p));
			isOpen.value = true;
			return;
		}

		editingIdProveedor.value = id;
		Object.assign(form, emptyForm(), {
			nombreEmpresa: p?.nombreEmpresa ?? "",
			personaContacto: p?.personaContacto ?? "",
			email: p?.email ?? "",
			telefono: p?.telefono ?? "",
			direccion: p?.direccion ?? "",
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
		apiError.value = "";
		const err = validate();
		if (err) { apiError.value = err; return; }

		saving.value = true;
		try {
			const payload = {
				nombreEmpresa: form.nombreEmpresa,
				personaContacto: form.personaContacto,
				email: form.email,
				telefono: form.telefono,
				direccion: form.direccion,
			};

			// CREATE
			if (mode.value === "create") {
				const res = await fetch(PROVIDERS_ENDPOINT, {
					method: "POST",
					headers: { "Content-Type": "application/json" },
					body: JSON.stringify(payload),
				});
				if (!res.ok) throw await readApiError(res);

				let created = null;
				try { created = await res.json(); } catch { created = payload; }

				// Si la API devuelve el registro (con idProveedor), lo agregamos
				// Si devuelve vacío, recargamos desde la API
				if (created && (created.idProveedor || created.nombreEmpresa)) {
					providers.value.unshift(normalizeProvider(created));
				} else {
					await loadProviders();
				}

				closeModal();
				return;
			}

			// EDIT
			if (!editingIdProveedor.value) {
				apiError.value = "No hay idProveedor para editar. Verifica que GET /api/Proveedores devuelva idProveedor.";
				return;
			}

			const url = `${PROVIDERS_ENDPOINT}/${encodeURIComponent(editingIdProveedor.value)}`;
			const res = await fetch(url, {
				method: "PUT",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify(payload),
			});
			if (!res.ok) throw await readApiError(res);

			// Muchas APIs retornan 204 NoContent: en ese caso recargamos
			if (res.status === 204) {
				await loadProviders();
				closeModal();
				return;
			}

			let updated = null;
			try { updated = await res.json(); } catch { updated = null; }

			if (updated) {
				const idx = providers.value.findIndex((x) => Number(x?.idProveedor) === Number(editingIdProveedor.value));
				if (idx !== -1) providers.value[idx] = { ...providers.value[idx], ...normalizeProvider(updated) };
			} else {
				await loadProviders();
			}

			closeModal();
		} catch (e) {
			apiError.value = e?.message ?? "Error guardando el proveedor.";
		} finally {
			saving.value = false;
		}
	}

	async function removeProvider(p) {
		const id = p?.idProveedor ?? null;
		const name = p?.nombreEmpresa ?? "este proveedor";

		if (!id) {
			alert("Este registro no tiene 'idProveedor'. DELETE requiere /api/Proveedores/{id}.");
			return;
		}

		if (!confirm(`¿Seguro que deseas eliminar ${name}?`)) return;

		try {
			const url = `${PROVIDERS_ENDPOINT}/${encodeURIComponent(id)}`;
			const res = await fetch(url, { method: "DELETE" });
			if (!res.ok) throw await readApiError(res);

			// Si devuelve 204, quitamos localmente
			providers.value = providers.value.filter((x) => Number(x?.idProveedor) !== Number(id));
		} catch (e) {
			alert(e?.message ?? "No se pudo eliminar.");
		}
	}
</script>

<style scoped>
	/* tu mismo CSS, solo agregué .mutedLine */
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