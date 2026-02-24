<template>
	<div class="page">
		<div class="content">
			<div class="hdr">
				<div class="hdr-left">
					<div class="cube">
						<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
							<path d="M16 11a4 4 0 1 0-8 0 4 4 0 0 0 8 0Z" stroke="currentColor" stroke-width="1.8" />
							<path d="M4 20c.7-3.4 4-5 8-5s7.3 1.6 8 5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
						</svg>
					</div>
					<div class="h1">Gestión de Usuarios</div>
				</div>

				<button v-if="canEdit" class="btn-primary" type="button" @click="openCreate">
					<span class="plus">＋</span>
					Nuevo Usuario
				</button>
			</div>

			<div v-if="loadError" class="alert">{{ loadError }}</div>

			<div class="search">
				<div class="search-ic">🔍</div>
				<input v-model="search" class="search-in" placeholder="Buscar usuarios..." />
			</div>

			<div class="stats4">
				<div class="stat stat-soft">
					<div class="stat-num blue">{{ totalUsuarios }}</div>
					<div class="stat-lbl">Total Usuarios</div>
				</div>

				<div class="stat stat-soft purpleBg">
					<div class="stat-num purple">{{ totalAdmins }}</div>
					<div class="stat-lbl">Administradores</div>
				</div>

				<div class="stat stat-soft greenBg">
					<div class="stat-num green">{{ totalActivos }}</div>
					<div class="stat-lbl">Activos</div>
				</div>

				<div class="stat stat-soft orangeBg">
					<div class="stat-num orange">{{ totalAuditores }}</div>
					<div class="stat-lbl">Auditores</div>
				</div>
			</div>

			<div class="card">
				<div v-if="loading" class="mutedLine">Cargando usuarios...</div>
				<div v-else-if="filteredRows.length === 0" class="mutedLine">No hay usuarios para mostrar.</div>

				<div v-else class="table">
					<div class="thead usersHead">
						<div>Usuario</div>
						<div>Email</div>
						<div>Rol</div>
						<div>Estado</div>
						<div>Último Acceso</div>
						<div class="actions-h">Acciones</div>
					</div>

					<div class="trow usersRow" v-for="u in visibleRows" :key="rowKey(u)">
						<div class="uCell">
							<div class="avatar" :class="avatarClass(u)">
								<span>{{ avatarLetter(u) }}</span>
							</div>
							<div class="uInfo">
								<div class="uName">{{ displayName(u) }}</div>
							</div>
						</div>

						<div class="muted">{{ (u.correo ?? u.email) ?? "-" }}</div>

						<div>
							<span class="chip" :class="roleChipClass(u)">
								<span class="dot"></span>
								{{ roleName(u) }}
							</span>
						</div>

						<div class="state">
							<span class="sDot" :class="isActive(u) ? 'on' : 'off'"></span>
							<span class="muted2">{{ isActive(u) ? "Activo" : "Inactivo" }}</span>

							<button class="miniBtn"
									type="button"
									@click="toggleEstado(u)"
									:disabled="!canEdit || rowBusyId === (u?.idUsuario ?? null)"
									:title="!canEdit ? 'Solo lectura' : ''">
								{{ isActive(u) ? "Desactivar" : "Activar" }}
							</button>
						</div>

						<div class="muted">{{ lastAccess(u) }}</div>

						<div class="actions">
							<template v-if="canEdit">
								<button class="icon-btn edit" type="button" title="Editar" aria-label="Editar" @click="openEdit(u)">✎</button>
								<button class="icon-btn key" type="button" title="Cambiar password" aria-label="Cambiar password" @click="openPassword(u)">🔑</button>
								<button class="icon-btn del" type="button" title="Eliminar" aria-label="Eliminar" @click="removeUser(u)">🗑</button>
							</template>

							<template v-else>
								<span class="mutedReadOnly">Solo lectura</span>
							</template>
						</div>
					</div>

					<div class="tfoot">
						<div class="foot-left">Mostrando {{ visibleRows.length }} de {{ filteredRows.length }} usuarios</div>

						<button class="foot-right" type="button" @click="toggleViewAll" :disabled="filteredRows.length <= pageSize">
							{{ viewAll ? "Ver Menos" : "Ver Todos" }} <span class="arrow">›</span>
						</button>
					</div>
				</div>
			</div>

			<div v-if="isOpen" class="modalOverlay" @click.self="closeModal">
				<div class="modal" role="dialog" aria-modal="true">
					<div class="modalHead">
						<div class="modalTitle">{{ mode === "create" ? "Nuevo Usuario" : "Editar Usuario" }}</div>
						<button class="xBtn" type="button" @click="closeModal" aria-label="Cerrar">×</button>
					</div>

					<div class="modalBody">
						<div v-if="apiError" class="alert">{{ apiError }}</div>

						<div class="grid2">
							<div class="field span2">
								<label>Nombre Completo</label>
								<input v-model.trim="form.nombreCompleto" autocomplete="off" placeholder="Ej: Juan Pérez" />
							</div>
						</div>

						<div class="field">
							<label>Correo</label>
							<input v-model.trim="form.correo" autocomplete="off" />
						</div>

						<div class="grid2">
							<div class="field">
								<label>Rol</label>
								<select v-model.number="form.idRol" :disabled="rolesLoading">
									<option :value="null" disabled>
										{{ rolesLoading ? "Cargando roles..." : "Seleccione un rol" }}
									</option>

									<option v-for="r in roles" :key="String(r.idRol)" :value="r.idRol">
										{{ r.nombre }}
									</option>
								</select>

								<div v-if="!rolesLoading && rolesLoadedOnce && roles.length === 0" class="hint">
									No hay roles registrados en la base de datos.
								</div>
							</div>

							<div class="field">
								<label>Estado</label>
								<select v-model="form.estado">
									<option value="Activo">Activo</option>
									<option value="Inactivo">Inactivo</option>
								</select>
							</div>
						</div>

						<div v-if="mode === 'create'" class="grid2">
							<div class="field">
								<label>Contraseña</label>
								<input v-model.trim="form.contrasena" type="password" autocomplete="new-password" />
							</div>
							<div class="field">
								<label>Confirmar</label>
								<input v-model.trim="form.contrasena2" type="password" autocomplete="new-password" />
							</div>
						</div>
					</div>

					<div class="modalFoot">
						<button class="btnLink" type="button" @click="closeModal">Cancelar</button>

						<button class="btnPrimary" type="button" :disabled="saving || rolesLoading" @click="saveUser">
							{{ saving ? (mode === "create" ? "Creando..." : "Guardando...") : (mode === "create" ? "Crear Usuario" : "Guardar Cambios") }}
						</button>
					</div>
				</div>
			</div>

			<div v-if="isPwdOpen" class="modalOverlay" @click.self="closePwd">
				<div class="modal" role="dialog" aria-modal="true">
					<div class="modalHead">
						<div class="modalTitle">Cambiar Password</div>
						<button class="xBtn" type="button" @click="closePwd" aria-label="Cerrar">×</button>
					</div>

					<div class="modalBody">
						<div v-if="pwdError" class="alert">{{ pwdError }}</div>

						<div class="field">
							<label>Usuario</label>
							<input :value="pwdUserLabel" disabled />
						</div>

						<div class="grid2">
							<div class="field">
								<label>Nueva contraseña</label>
								<input v-model.trim="pwdForm.password" type="password" autocomplete="new-password" />
							</div>
							<div class="field">
								<label>Confirmar</label>
								<input v-model.trim="pwdForm.password2" type="password" autocomplete="new-password" />
							</div>
						</div>
					</div>

					<div class="modalFoot">
						<button class="btnLink" type="button" @click="closePwd">Cancelar</button>
						<button class="btnPrimary" type="button" :disabled="pwdSaving" @click="savePassword">
							{{ pwdSaving ? "Guardando..." : "Guardar Password" }}
						</button>
					</div>
				</div>
			</div>

		</div>
	</div>
</template>

<script setup>
	import { computed, onMounted, reactive, ref } from "vue";
	import { getPermsSafe } from "../router/auth.service";

	const perms = computed(() => getPermsSafe());
	const canEdit = computed(() => perms.value?.canEditAll === true);

	const API_BASE = "https://localhost:7198";
	const USERS_ENDPOINT = `${API_BASE}/api/Usuarios`;
	const ROLES_ENDPOINT = `${API_BASE}/api/Roles`;
	const RESET_PASSWORD_ENDPOINT = `${API_BASE}/api/Auth/reset-password`;

	const search = ref("");
	const isOpen = ref(false);
	const saving = ref(false);
	const apiError = ref("");

	const loading = ref(false);
	const loadError = ref("");

	const viewAll = ref(false);
	const pageSize = 4;

	const mode = ref("create");
	const editingId = ref(null);

	const rows = ref([]);
	const roles = ref([]);

	const rolesLoading = ref(false);
	const rolesLoadedOnce = ref(false);

	const isPwdOpen = ref(false);
	const pwdSaving = ref(false);
	const pwdError = ref("");
	const pwdUserId = ref(null);
	const pwdUserLabel = ref("");
	const pwdUsername = ref("");
	const pwdForm = reactive({ password: "", password2: "" });

	const rowBusyId = ref(null);

	const emptyForm = () => ({
		idUsuario: null,
		nombreCompleto: "",
		correo: "",
		idRol: null,
		estado: "Activo",
		contrasena: "",
		contrasena2: "",
	});
	const form = reactive(emptyForm());

	onMounted(async () => {
		await loadAll();
	});

	function getToken() {
		return localStorage.getItem("sm_token") || sessionStorage.getItem("sm_token") || "";
	}

	function extractFirstValidationMessage(obj) {
		const errors = obj?.errors;
		if (!errors || typeof errors !== "object") return "";
		for (const k of Object.keys(errors)) {
			const v = errors[k];
			if (Array.isArray(v) && v.length) return String(v[0]);
			if (typeof v === "string" && v.trim()) return v.trim();
		}
		return "";
	}

	async function readApiError(res) {
		let data = null;
		let text = "";
		try {
			const ct = res.headers.get("content-type") || "";
			if (ct.includes("application/json")) {
				data = await res.json();
				const first = extractFirstValidationMessage(data);
				text =
					first ||
					data?.message ||
					data?.msg ||
					data?.error ||
					data?.title ||
					(data?.errors ? JSON.stringify(data.errors) : "") ||
					JSON.stringify(data);
			} else {
				text = await res.text();
			}
		} catch { }

		const msg = text?.trim()
			? `${res.status} ${res.statusText}: ${text}`
			: `${res.status} ${res.statusText}`;

		return new Error(msg);
	}

	async function apiFetch(url, options = {}) {
		const token = getToken();
		const headers = new Headers(options.headers || {});
		if (!headers.has("Content-Type") && options.body != null) headers.set("Content-Type", "application/json");
		if (token) headers.set("Authorization", `Bearer ${token}`);

		const res = await fetch(url, { ...options, headers });
		if (!res.ok) throw await readApiError(res);

		const ct = res.headers.get("content-type") || "";
		if (ct.includes("application/json")) {
			try {
				return await res.json();
			} catch {
				return null;
			}
		}
		return null;
	}

	function normalizeRole(r) {
		const idRol =
			r?.idRol ??
			r?.idRole ??
			r?.IdRol ??
			r?.IdRole ??
			r?.id ??
			r?.rolId ??
			r?.roleId ??
			null;

		const nombre =
			r?.nombre ??
			r?.name ??
			r?.descripcion ??
			r?.Nombre ??
			r?.Description ??
			"";

		return { ...r, idRol, nombre };
	}

	function normalizeUser(u) {
		const idUsuario = u?.idUsuario ?? u?.id ?? u?.usuarioId ?? u?.userId ?? null;

		const nombreCompleto =
			(u?.nombreCompleto ?? u?.fullName ?? u?.nombre ?? u?.NombreCompleto ?? u?.Nombre ?? "")?.toString().trim() ||
			`${u?.nombres ?? ""} ${u?.apellidos ?? ""}`.trim();

		const correo = u?.correo ?? u?.email ?? u?.mail ?? null;

		const idRol =
			u?.idRol ??
			u?.idRole ??
			u?.IdRol ??
			u?.IdRole ??
			u?.rolId ??
			u?.roleId ??
			u?.rol?.idRol ??
			u?.rol?.idRole ??
			u?.rol?.IdRol ??
			u?.rol?.IdRole ??
			u?.rol?.id ??
			null;

		const estadoRaw = u?.estado ?? u?.activo ?? u?.isActive ?? u?.active ?? null;
		const estadoStr = String(estadoRaw ?? "").toLowerCase();

		const activoBool =
			typeof estadoRaw === "boolean"
				? estadoRaw
				: estadoStr === "activo" || estadoStr === "true" || estadoStr === "1";

		const ultimoAcceso =
			u?.ultimoAcceso ?? u?.lastAccess ?? u?.lastLogin ?? u?.fechaUltimoAcceso ?? null;

		const rolNombre =
			(typeof u?.rol === "string" ? u.rol : (u?.rol?.nombre ?? u?.rol?.name ?? u?.rol?.Nombre)) ??
			u?.rolNombre ??
			null;

		const username =
			u?.username ?? u?.userName ?? u?.Usuario ?? u?.usuario ?? null;

		return {
			...u,
			idUsuario,
			nombreCompleto,
			correo,
			idRol,
			rolNombre,
			activo: activoBool,
			estado: activoBool ? "Activo" : "Inactivo",
			ultimoAcceso,
			username,
		};
	}

	function normalizeList(data) {
		if (Array.isArray(data)) return data;
		if (Array.isArray(data?.items)) return data.items;
		if (Array.isArray(data?.data)) return data.data;
		if (Array.isArray(data?.result)) return data.result;
		if (Array.isArray(data?.$values)) return data.$values;
		return [];
	}

	async function loadAll() {
		loading.value = true;
		loadError.value = "";
		try {
			await Promise.all([loadRoles(), loadUsers()]);
		} catch (e) {
			loadError.value = e?.message ?? "Error cargando datos.";
		} finally {
			loading.value = false;
		}
	}

	async function loadRoles() {
		rolesLoading.value = true;
		rolesLoadedOnce.value = true;
		try {
			const data = await apiFetch(ROLES_ENDPOINT, { method: "GET" });
			const list = normalizeList(data).map(normalizeRole);
			roles.value = list.filter((x) => x.idRol != null);
		} finally {
			rolesLoading.value = false;
		}
	}

	async function loadUsers() {
		const data = await apiFetch(USERS_ENDPOINT, { method: "GET" });
		rows.value = normalizeList(data).map(normalizeUser);
	}

	function rowKey(u) {
		return String(u?.idUsuario ?? u?.correo ?? u?.nombreCompleto ?? Math.random());
	}

	const filteredRows = computed(() => {
		const q = search.value.trim().toLowerCase();
		if (!q) return rows.value;
		return rows.value.filter((u) => {
			return (
				String(displayName(u)).toLowerCase().includes(q) ||
				String(u.correo ?? "").toLowerCase().includes(q) ||
				String(roleName(u) ?? "").toLowerCase().includes(q) ||
				String(u.estado ?? "").toLowerCase().includes(q)
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

	function roleName(u) {
		if (u?.rolNombre) return u.rolNombre;
		const r = roles.value.find((x) => Number(x.idRol) === Number(u?.idRol));
		return r?.nombre ?? "-";
	}

	function isActive(u) {
		return Boolean(u?.activo ?? (String(u?.estado ?? "").toLowerCase() === "activo"));
	}

	function lastAccess(u) {
		return u?.ultimoAcceso ? String(u.ultimoAcceso).slice(0, 10) : "-";
	}

	function displayName(u) {
		const full = u?.nombreCompleto ?? u?.fullName ?? null;
		if (full && String(full).trim()) return String(full).trim();
		return "Usuario";
	}

	function avatarLetter(u) {
		const name = displayName(u).trim();
		return name ? name[0].toUpperCase() : "U";
	}

	function avatarClass(u) {
		const n = (displayName(u).length + (u?.correo?.length ?? 0)) % 2;
		return n === 0 ? "av-purple" : "av-blue";
	}

	function roleChipClass(u) {
		const rn = String(roleName(u)).toLowerCase();
		if (rn.includes("admin")) return "chip-admin";
		if (rn.includes("audit")) return "chip-auditor";
		return "chip-user";
	}

	const totalUsuarios = computed(() => rows.value.length);
	const totalAdmins = computed(() =>
		rows.value.reduce((acc, u) => acc + (String(roleName(u)).toLowerCase().includes("admin") ? 1 : 0), 0)
	);
	const totalActivos = computed(() =>
		rows.value.reduce((acc, u) => acc + (isActive(u) ? 1 : 0), 0)
	);
	const totalAuditores = computed(() =>
		rows.value.reduce((acc, u) => acc + (String(roleName(u)).toLowerCase().includes("audit") ? 1 : 0), 0)
	);

	function openCreate() {
		if (!canEdit.value) return;

		apiError.value = "";
		mode.value = "create";
		editingId.value = null;

		Object.assign(form, emptyForm(), {
			idRol: roles.value?.[0]?.idRol ?? null,
		});

		isOpen.value = true;
	}

	function openEdit(u) {
		if (!canEdit.value) return;

		apiError.value = "";
		mode.value = "edit";

		const id = u?.idUsuario ?? null;
		editingId.value = id;

		Object.assign(form, emptyForm(), {
			idUsuario: id,
			nombreCompleto: (u?.nombreCompleto ?? displayName(u) ?? "").toString(),
			correo: u?.correo ?? "",
			idRol: u?.idRol ?? (roles.value?.[0]?.idRol ?? null),
			estado: isActive(u) ? "Activo" : "Inactivo",
		});

		isOpen.value = true;
	}

	function closeModal() {
		isOpen.value = false;
	}

	function validate() {
		if (!form.nombreCompleto) return "Nombre Completo es obligatorio.";
		if (!form.correo) return "Correo es obligatorio.";
		if (!form.idRol) return "Debes seleccionar un Rol.";
		if (!/^\S+@\S+\.\S+$/.test(form.correo)) return "Correo inválido.";

		if (mode.value === "create") {
			if (!form.contrasena) return "Contraseña es obligatoria.";
			if (form.contrasena.length < 6) return "Contraseña debe tener al menos 6 caracteres.";
			if (form.contrasena !== form.contrasena2) return "Las contraseñas no coinciden.";
		}
		return "";
	}

	function deriveUsernameFromEmail(email) {
		const e = (email ?? "").toString().trim();
		if (!e.includes("@")) return e || "user";
		return e.split("@")[0] || "user";
	}

	function extractIdFromCreateResponse(obj) {
		return obj?.idUsuario ?? obj?.id ?? obj?.usuarioId ?? obj?.userId ?? null;
	}

	function buildOptimisticUser(createdRaw) {
		const id = extractIdFromCreateResponse(createdRaw);
		const r = roles.value.find((x) => Number(x.idRol) === Number(form.idRol));

		return normalizeUser({
			...(createdRaw || {}),
			idUsuario: id,
			nombreCompleto: form.nombreCompleto,
			fullName: form.nombreCompleto,
			correo: form.correo,
			email: form.correo,
			idRol: Number(form.idRol),
			idRole: Number(form.idRol),
			rolNombre: r?.nombre ?? null,
			activo: form.estado === "Activo",
			estado: form.estado,
			username: deriveUsernameFromEmail(form.correo),
		});
	}

	function buildFullUpdatePayloadFromRow(u, overrides = {}) {
		const email = (u?.correo ?? u?.email ?? "").toString().trim();
		const nombreCompleto = (u?.nombreCompleto ?? u?.fullName ?? u?.nombre ?? displayName(u) ?? "").toString().trim();
		const username = (u?.username ?? u?.userName ?? "").toString().trim() || deriveUsernameFromEmail(email);

		const idRol =
			u?.idRol ??
			u?.idRole ??
			u?.rolId ??
			u?.roleId ??
			roles.value?.[0]?.idRol ??
			null;

		const activo =
			overrides.activo != null
				? Boolean(overrides.activo)
				: Boolean(u?.activo ?? (String(u?.estado ?? "").toLowerCase() === "activo"));

		const estado = overrides.estado != null ? String(overrides.estado) : (activo ? "Activo" : "Inactivo");

		const payload = {
			nombreCompleto,
			NombreCompleto: nombreCompleto,
			fullName: nombreCompleto,
			nombre: nombreCompleto,

			email,
			Email: email,
			correo: email,

			username,
			Username: username,

			idRol: idRol != null ? Number(idRol) : null,
			idRole: idRol != null ? Number(idRol) : null,

			activo,
			estado,
		};

		for (const k of Object.keys(overrides)) payload[k] = overrides[k];

		return payload;
	}

	async function saveUser() {
		if (!canEdit.value) return;

		apiError.value = "";
		const err = validate();
		if (err) {
			apiError.value = err;
			return;
		}

		saving.value = true;
		try {
			const username = deriveUsernameFromEmail(form.correo);

			const payload = {
				nombreCompleto: form.nombreCompleto,
				NombreCompleto: form.nombreCompleto,
				fullName: form.nombreCompleto,
				nombre: form.nombreCompleto,

				email: form.correo,
				Email: form.correo,
				correo: form.correo,

				username,
				Username: username,

				idRole: Number(form.idRol),
				idRol: Number(form.idRol),

				estado: form.estado,
				activo: form.estado === "Activo",
			};

			if (mode.value === "create") {
				payload.password = form.contrasena;
				payload.contrasena = form.contrasena;
			}

			if (mode.value === "create") {
				const createdRaw = await apiFetch(USERS_ENDPOINT, {
					method: "POST",
					body: JSON.stringify(payload),
				});

				rows.value.unshift(buildOptimisticUser(createdRaw));
				closeModal();
				await loadUsers();
				return;
			}

			if (!editingId.value) {
				apiError.value = "No se encontró idUsuario para editar.";
				return;
			}

			const updated = await apiFetch(`${USERS_ENDPOINT}/${encodeURIComponent(editingId.value)}`, {
				method: "PUT",
				body: JSON.stringify(payload),
			});

			if (updated) {
				const n = normalizeUser(updated);
				const idx = rows.value.findIndex((x) => Number(x?.idUsuario) === Number(editingId.value));
				if (idx !== -1) rows.value[idx] = { ...rows.value[idx], ...n };
			} else {
				await loadUsers();
			}

			closeModal();
		} catch (e) {
			apiError.value = e?.message ?? "Error guardando el usuario.";
		} finally {
			saving.value = false;
		}
	}

	async function removeUser(u) {
		if (!canEdit.value) return;

		const id = u?.idUsuario ?? null;
		const name = displayName(u);

		if (!id) {
			alert("Este registro no tiene idUsuario. No se puede eliminar sin id.");
			return;
		}

		const ok = confirm(`¿Seguro que deseas eliminar ${name}?`);
		if (!ok) return;

		try {
			await apiFetch(`${USERS_ENDPOINT}/${encodeURIComponent(id)}`, { method: "DELETE" });
			rows.value = rows.value.filter((x) => Number(x?.idUsuario) !== Number(id));
		} catch (e) {
			alert(e?.message ?? "No se pudo eliminar.");
		}
	}

	async function toggleEstado(u) {
		if (!canEdit.value) return;

		const id = u?.idUsuario ?? null;
		if (!id) return;

		const email = (u?.correo ?? u?.email ?? "").toString().trim();
		const nombreCompleto = (u?.nombreCompleto ?? u?.fullName ?? u?.nombre ?? displayName(u) ?? "").toString().trim();

		if (!email || !nombreCompleto) {
			alert("No puedo activar/desactivar porque a este usuario le faltan datos requeridos (email/nombreCompleto) en la respuesta de la API.");
			return;
		}

		const newActive = !isActive(u);
		const payload = buildFullUpdatePayloadFromRow(u, { activo: newActive, estado: newActive ? "Activo" : "Inactivo" });

		rowBusyId.value = id;

		const idx = rows.value.findIndex((x) => Number(x?.idUsuario) === Number(id));
		const prev = idx !== -1 ? { ...rows.value[idx] } : null;

		if (idx !== -1) rows.value[idx] = { ...rows.value[idx], activo: newActive, estado: newActive ? "Activo" : "Inactivo" };

		try {
			const updated = await apiFetch(`${USERS_ENDPOINT}/${encodeURIComponent(id)}`, {
				method: "PUT",
				body: JSON.stringify(payload),
			});

			if (updated && idx !== -1) rows.value[idx] = { ...rows.value[idx], ...normalizeUser(updated) };
		} catch (e) {
			if (idx !== -1 && prev) rows.value[idx] = prev;
			alert(e?.message ?? "No se pudo cambiar el estado.");
		} finally {
			rowBusyId.value = null;
		}
	}

	function openPassword(u) {
		if (!canEdit.value) return;

		pwdError.value = "";
		pwdSaving.value = false;

		const id = u?.idUsuario ?? null;
		if (!id) {
			alert("Este registro no tiene idUsuario. No se puede cambiar password sin id.");
			return;
		}

		const email = (u?.correo ?? u?.email ?? "").toString().trim();
		const username = (u?.username ?? u?.userName ?? "").toString().trim() || deriveUsernameFromEmail(email);

		pwdUserId.value = id;
		pwdUsername.value = username;
		pwdUserLabel.value = `${displayName(u)} (${email || "-"})`;
		pwdForm.password = "";
		pwdForm.password2 = "";
		isPwdOpen.value = true;
	}

	function closePwd() {
		isPwdOpen.value = false;
	}

	function validatePwd() {
		if (!pwdForm.password) return "La contraseña es obligatoria.";
		if (pwdForm.password.length < 6) return "Debe tener al menos 6 caracteres.";
		if (pwdForm.password !== pwdForm.password2) return "Las contraseñas no coinciden.";
		if (!pwdUsername.value) return "No se encontró username para este usuario.";
		return "";
	}

	async function savePassword() {
		if (!canEdit.value) return;

		pwdError.value = "";
		const err = validatePwd();
		if (err) {
			pwdError.value = err;
			return;
		}

		pwdSaving.value = true;
		try {
			await apiFetch(RESET_PASSWORD_ENDPOINT, {
				method: "POST",
				body: JSON.stringify({
					username: pwdUsername.value,
					newPassword: pwdForm.password,
				}),
			});

			isPwdOpen.value = false;
			pwdForm.password = "";
			pwdForm.password2 = "";
		} catch (e) {
			pwdError.value = e?.message ?? "No se pudo cambiar el password.";
		} finally {
			pwdSaving.value = false;
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

	.mutedLine {
		color: #64748b;
		font-weight: 800;
		padding: 10px 2px;
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
		background: rgba(99, 102, 241, 0.10);
		border: 1px solid rgba(99, 102, 241, 0.16);
		display: grid;
		place-items: center;
		color: #6366f1;
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

	.mutedReadOnly {
		color: #64748b;
		font-weight: 900;
		font-size: 12px;
		padding: 6px 10px;
		border-radius: 999px;
		background: rgba(15, 23, 42, 0.04);
		border: 1px solid rgba(15, 23, 42, 0.06);
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

	.stats4 {
		display: grid;
		grid-template-columns: repeat(4, minmax(0, 1fr));
		gap: 14px;
		margin-bottom: 16px;
	}

	.stat {
		border-radius: 14px;
		padding: 16px 18px;
		border: 1px solid rgba(15, 23, 42, 0.06);
		box-shadow: 0 12px 24px rgba(10, 20, 70, 0.06);
	}

	.stat-soft {
		background: rgba(255, 255, 255, 0.7);
	}

	.purpleBg {
		background: rgba(124, 58, 237, 0.06);
	}

	.greenBg {
		background: rgba(34, 197, 94, 0.06);
	}

	.orangeBg {
		background: rgba(249, 115, 22, 0.06);
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

	.blue {
		color: #2563eb;
	}

	.purple {
		color: #7c3aed;
	}

	.green {
		color: #16a34a;
	}

	.orange {
		color: #f97316;
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
		gap: 14px;
		padding: 16px 12px;
		border-bottom: 1px solid rgba(15, 23, 42, 0.05);
		align-items: center;
		font-size: 13px;
	}

	.usersHead {
		grid-template-columns: 2fr 2fr 1.2fr 1.3fr 1.2fr 1fr;
	}

	.usersRow {
		grid-template-columns: 2fr 2fr 1.2fr 1.3fr 1.2fr 1fr;
	}

	.muted {
		color: #64748b;
		font-weight: 800;
		font-size: 13px;
	}

	.muted2 {
		color: #475569;
		font-weight: 800;
		font-size: 13px;
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

		.icon-btn.key {
			color: #7c3aed;
		}

	.uCell {
		display: flex;
		align-items: center;
		gap: 12px;
		min-width: 0;
	}

	.avatar {
		width: 38px;
		height: 38px;
		border-radius: 999px;
		display: grid;
		place-items: center;
		font-weight: 900;
		color: #fff;
	}

	.av-purple {
		background: rgba(124, 58, 237, 0.9);
	}

	.av-blue {
		background: rgba(59, 130, 246, 0.9);
	}

	.uInfo {
		min-width: 0;
	}

	.uName {
		font-weight: 900;
		color: #0f172a;
	}

	.chip {
		display: inline-flex;
		align-items: center;
		gap: 8px;
		padding: 7px 10px;
		border-radius: 999px;
		font-weight: 900;
		font-size: 12px;
		border: 1px solid rgba(15, 23, 42, 0.06);
	}

		.chip .dot {
			width: 8px;
			height: 8px;
			border-radius: 999px;
			display: inline-block;
		}

	.chip-admin {
		background: rgba(124, 58, 237, 0.10);
		color: #7c3aed;
	}

		.chip-admin .dot {
			background: #7c3aed;
		}

	.chip-user {
		background: rgba(59, 130, 246, 0.10);
		color: #2563eb;
	}

		.chip-user .dot {
			background: #2563eb;
		}

	.chip-auditor {
		background: rgba(249, 115, 22, 0.10);
		color: #f97316;
	}

		.chip-auditor .dot {
			background: #f97316;
		}

	.state {
		display: flex;
		align-items: center;
		gap: 10px;
		flex-wrap: wrap;
	}

	.sDot {
		width: 10px;
		height: 10px;
		border-radius: 999px;
		background: #cbd5e1;
	}

		.sDot.on {
			background: #22c55e;
		}

		.sDot.off {
			background: #ef4444;
		}

	.miniBtn {
		border: 1px solid rgba(15, 23, 42, 0.10);
		background: rgba(255, 255, 255, 0.9);
		border-radius: 10px;
		padding: 6px 10px;
		font-weight: 900;
		cursor: pointer;
		color: #334155;
	}

		.miniBtn:disabled {
			opacity: 0.55;
			cursor: not-allowed;
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
			opacity: 0.55;
			cursor: not-allowed;
		}

	.arrow {
		font-size: 18px;
	}

	.modalOverlay {
		position: fixed;
		inset: 0;
		background: rgba(15, 23, 42, 0.25);
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
		box-shadow: 0 18px 40px rgba(0, 0, 0, 0.22);
		border: 1px solid rgba(15, 23, 42, 0.10);
		overflow: hidden;
	}

	.modalHead {
		height: 64px;
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0 18px;
		border-bottom: 1px solid rgba(15, 23, 42, 0.10);
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
		grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
		column-gap: 22px;
		row-gap: 14px;
	}

	.span2 {
		grid-column: 1 / -1;
	}

	.field label {
		display: block;
		margin-bottom: 8px;
		font-weight: 800;
		color: #64748b;
	}

	.field input,
	.field select {
		width: 100%;
		box-sizing: border-box;
		border: 1px solid rgba(148, 163, 184, 0.55);
		border-radius: 10px;
		padding: 12px 14px;
		font-size: 14px;
		outline: none;
		background: #fff;
		transition: border-color 0.15s ease, box-shadow 0.15s ease;
	}

		.field input:focus,
		.field select:focus {
			border-color: rgba(59, 130, 246, 0.65);
			box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.18);
		}

	.hint {
		margin-top: 8px;
		font-weight: 800;
		color: #b91c1c;
		background: rgba(239, 68, 68, 0.08);
		border: 1px solid rgba(239, 68, 68, 0.25);
		padding: 8px 10px;
		border-radius: 10px;
		font-size: 12px;
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
		background: linear-gradient(180deg, #2f74ff, #1e5ae9);
		box-shadow: 0 14px 28px rgba(37, 99, 235, 0.25);
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
		border-radius: 10px;
		font-weight: 700;
	}

	@media (max-width: 1100px) {
		.stats4 {
			grid-template-columns: 1fr;
		}

		.usersHead,
		.usersRow {
			grid-template-columns: 1fr;
		}

		.actions-h {
			text-align: left;
		}

		.actions {
			justify-content: flex-start;
		}

		.grid2 {
			grid-template-columns: 1fr;
		}
	}
</style>