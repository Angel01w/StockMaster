<template>
	<div class="page">
		<div class="content">
			<!-- HEADER -->
			<div class="hdr">
				<div class="hdr-left">
					<div class="cube">
						<!-- icon users -->
						<svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
							<path d="M16 11a4 4 0 1 0-8 0 4 4 0 0 0 8 0Z"
								  stroke="currentColor"
								  stroke-width="1.8" />
							<path d="M4 20c.7-3.4 4-5 8-5s7.3 1.6 8 5"
								  stroke="currentColor"
								  stroke-width="1.8"
								  stroke-linecap="round" />
						</svg>
					</div>
					<div class="h1">Gestión de Usuarios</div>
				</div>

				<button class="btn-primary" type="button" @click="openCreate">
					<span class="plus">＋</span>
					Nuevo Usuario
				</button>
			</div>

			<!-- SEARCH -->
			<div class="search">
				<div class="search-ic">🔍</div>
				<input v-model="search" class="search-in" placeholder="Buscar usuarios..." />
			</div>

			<!-- STATS -->
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

			<!-- TABLE CARD -->
			<div class="card">
				<div class="table">
					<div class="thead usersHead">
						<div>Usuario</div>
						<div>Email</div>
						<div>Rol</div>
						<div>Estado</div>
						<div>Último Acceso</div>
						<div class="actions-h">Acciones</div>
					</div>

					<div class="trow usersRow" v-for="u in visibleRows" :key="rowKey(u)">
						<!-- Usuario -->
						<div class="uCell">
							<div class="avatar" :class="avatarClass(u)">
								<span>{{ avatarLetter(u) }}</span>
							</div>
							<div class="uInfo">
								<div class="uName">{{ displayName(u) }}</div>
							</div>
						</div>

						<!-- Email -->
						<div class="muted">{{ u.email ?? "-" }}</div>

						<!-- Rol -->
						<div>
							<span class="chip" :class="roleChipClass(u)">
								<span class="dot"></span>
								{{ roleName(u) }}
							</span>
						</div>

						<!-- Estado -->
						<div class="state">
							<span class="sDot" :class="isActive(u) ? 'on' : 'off'"></span>
							<span class="muted2">{{ isActive(u) ? "Activo" : "Inactivo" }}</span>
						</div>

						<!-- Último acceso -->
						<div class="muted">{{ lastAccess(u) }}</div>

						<!-- Acciones -->
						<div class="actions">
							<button class="icon-btn edit" type="button" title="Editar" aria-label="Editar" @click="openEdit(u)">✎</button>
							<button class="icon-btn del" type="button" title="Eliminar" aria-label="Eliminar" @click="removeUser(u)">🗑</button>
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

			<!-- MODAL -->
			<div v-if="isOpen" class="modalOverlay" @click.self="closeModal">
				<div class="modal" role="dialog" aria-modal="true">
					<div class="modalHead">
						<div class="modalTitle">{{ mode === "create" ? "Nuevo Usuario" : "Editar Usuario" }}</div>
						<button class="xBtn" type="button" @click="closeModal" aria-label="Cerrar">×</button>
					</div>

					<div class="modalBody">
						<div v-if="apiError" class="alert">{{ apiError }}</div>

						<div class="field">
							<label>Nombre Completo</label>
							<input v-model.trim="form.nombreCompleto" autocomplete="off" />
						</div>

						<div class="field">
							<label>Email</label>
							<input v-model.trim="form.email" autocomplete="off" />
						</div>

						<div class="field">
							<label>Rol</label>
							<select v-model="form.idRol">
								<option :value="null" disabled>Seleccione un rol</option>
								<option v-for="r in roles" :key="String(r.id)" :value="r.id">
									{{ r.nombre }}
								</option>
							</select>
						</div>

						<div class="field">
							<label>Estado</label>
							<select v-model="form.estado">
								<option value="Activo">Activo</option>
								<option value="Inactivo">Inactivo</option>
							</select>
						</div>
					</div>

					<div class="modalFoot">
						<button class="btnLink" type="button" @click="closeModal">Cancelar</button>

						<button class="btnPrimary" type="button" :disabled="saving" @click="saveUser">
							{{ saving ? (mode === "create" ? "Creando..." : "Guardando...") : (mode === "create" ? "Crear Usuario" : "Guardar Cambios") }}
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

/** ===== API ===== */
const API_BASE = "https://localhost:7198";
const USERS_ENDPOINT = `${API_BASE}/api/Usuarios`;
const ROLES_ENDPOINT = `${API_BASE}/api/Roles`;

/** ===== STATE ===== */
const search = ref("");
const isOpen = ref(false);
const saving = ref(false);
const apiError = ref("");

const viewAll = ref(false);
const pageSize = 4;

const mode = ref("create"); // create | edit
const editingId = ref(null);

const rows = ref([]);
const roles = ref([]);

/** ===== FORM ===== */
const emptyForm = () => ({
  id: null,
  nombreCompleto: "",
  email: "",
  idRol: null,
  estado: "Activo",
});
const form = reactive(emptyForm());

onMounted(async () => {
  await Promise.all([loadRoles(), loadUsers()]);
});

/** ===== NORMALIZERS ===== */
function normalizeRole(r) {
  return {
    ...r,
    id: r?.id ?? r?.idRol ?? r?.rolId ?? r?.roleId ?? null,
    nombre: r?.nombre ?? r?.name ?? r?.descripcion ?? "Rol",
  };
}

function normalizeUser(u) {
  const id = u?.id ?? u?.idUsuario ?? u?.usuarioId ?? u?.userId ?? null;

  // rol puede venir como string, objeto, o idRol
  const roleObj = u?.rol ?? u?.role ?? u?.roles ?? null;
  const roleName =
    typeof roleObj === "string"
      ? roleObj
      : (roleObj?.nombre ?? roleObj?.name ?? null);

  const idRol = u?.idRol ?? u?.rolId ?? roleObj?.idRol ?? roleObj?.id ?? null;

  const nombreCompleto =
    u?.nombreCompleto ??
    u?.nombre ??
    u?.nombres ??
    u?.fullName ??
    u?.username ??
    "Usuario";

  const email = u?.email ?? u?.correo ?? u?.mail ?? null;

  const estadoRaw = u?.estado ?? u?.isActive ?? u?.activo ?? u?.active ?? null;
  const estado =
    typeof estadoRaw === "boolean"
      ? (estadoRaw ? "Activo" : "Inactivo")
      : (String(estadoRaw ?? "Activo"));

  const ultimoAcceso =
    u?.ultimoAcceso ?? u?.lastAccess ?? u?.lastLogin ?? u?.fechaUltimoAcceso ?? null;

  return {
    ...u,
    id,
    nombreCompleto,
    email,
    idRol,
    rolNombre: roleName,
    estado,
    ultimoAcceso,
  };
}

function normalizeList(data) {
  const list = Array.isArray(data) ? data : (data?.items ?? []);
  return list;
}

/** ===== LOADERS ===== */
async function loadRoles() {
  try {
    const res = await fetch(ROLES_ENDPOINT);
    if (!res.ok) throw new Error(`GET roles falló (${res.status})`);
    const data = await res.json();
    roles.value = normalizeList(data).map(normalizeRole);
    if (roles.value.length === 0) seedRolesFallback();
  } catch {
    seedRolesFallback();
  }
}

async function loadUsers() {
  try {
    const res = await fetch(USERS_ENDPOINT);
    if (!res.ok) throw new Error(`GET usuarios falló (${res.status})`);
    const data = await res.json();
    rows.value = normalizeList(data).map(normalizeUser);
    if (rows.value.length === 0) seedUsersFallback();
  } catch {
    seedUsersFallback();
  }
}

/** ===== FALLBACKS (solo UI) ===== */
function seedRolesFallback() {
  roles.value = [
    { id: 1, nombre: "Administrador" },
    { id: 2, nombre: "Usuario" },
    { id: 3, nombre: "Auditor" },
  ];
}

function seedUsersFallback() {
  rows.value = [
    {
      id: null,
      nombreCompleto: "Admin",
      email: "admin@stockmaster.com",
      idRol: 1,
      rolNombre: "Administrador",
      estado: "Activo",
      ultimoAcceso: "2024-04-25",
    },
    {
      id: null,
      nombreCompleto: "Jose Martinez",
      email: "jose@stockmaster.com",
      idRol: 2,
      rolNombre: "Usuario",
      estado: "Activo",
      ultimoAcceso: "2024-04-25",
    },
    {
      id: null,
      nombreCompleto: "Ana López",
      email: "ana@stockmaster.com",
      idRol: 2,
      rolNombre: "Usuario",
      estado: "Activo",
      ultimoAcceso: "2024-04-24",
    },
    {
      id: null,
      nombreCompleto: "Carlos Gómez",
      email: "carlos@stockmaster.com",
      idRol: 3,
      rolNombre: "Auditor",
      estado: "Activo",
      ultimoAcceso: "2024-04-23",
    },
  ];
}

/** ===== UI HELPERS ===== */
function rowKey(u) {
  return String(u?.id ?? u?.email ?? u?.nombreCompleto ?? Math.random());
}

const filteredRows = computed(() => {
  const q = search.value.trim().toLowerCase();
  if (!q) return rows.value;
  return rows.value.filter((u) => {
    return (
      String(u.nombreCompleto ?? "").toLowerCase().includes(q) ||
      String(u.email ?? "").toLowerCase().includes(q) ||
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
  // prioridad: rolNombre ya normalizado > lookup por idRol
  if (u?.rolNombre) return u.rolNombre;
  const r = roles.value.find((x) => x.id === u?.idRol);
  return r?.nombre ?? "Usuario";
}

function isActive(u) {
  const s = String(u?.estado ?? "Activo").toLowerCase();
  return s === "activo" || s === "true" || s === "1";
}

function lastAccess(u) {
  return u?.ultimoAcceso ? String(u.ultimoAcceso).slice(0, 10) : "-";
}

function displayName(u) {
  return u?.nombreCompleto ?? "Usuario";
}

function avatarLetter(u) {
  const name = displayName(u).trim();
  return name ? name[0].toUpperCase() : "U";
}

function avatarClass(u) {
  // alterna colores como la imagen (morado/azul)
  const n = (displayName(u).length + (u?.email?.length ?? 0)) % 2;
  return n === 0 ? "av-purple" : "av-blue";
}

function roleChipClass(u) {
  const rn = roleName(u).toLowerCase();
  if (rn.includes("admin")) return "chip-admin";
  if (rn.includes("audit")) return "chip-auditor";
  return "chip-user";
}

/** ===== KPIs ===== */
const totalUsuarios = computed(() => rows.value.length);

const totalAdmins = computed(() =>
  rows.value.reduce((acc, u) => acc + (roleName(u).toLowerCase().includes("admin") ? 1 : 0), 0)
);

const totalActivos = computed(() =>
  rows.value.reduce((acc, u) => acc + (isActive(u) ? 1 : 0), 0)
);

const totalAuditores = computed(() =>
  rows.value.reduce((acc, u) => acc + (roleName(u).toLowerCase().includes("audit") ? 1 : 0), 0)
);

/** ===== MODAL ACTIONS ===== */
function openCreate() {
  apiError.value = "";
  mode.value = "create";
  editingId.value = null;
  Object.assign(form, emptyForm(), {
    idRol: roles.value?.[1]?.id ?? roles.value?.[0]?.id ?? null,
  });
  isOpen.value = true;
}

function openEdit(u) {
  apiError.value = "";

  const id = u?.id ?? null;
  if (!id) {
    apiError.value = "Este registro no tiene 'idUsuario'. La API debe devolverlo para poder editar/eliminar.";
  }

  mode.value = "edit";
  editingId.value = id;

  Object.assign(form, emptyForm(), {
    id,
    nombreCompleto: u?.nombreCompleto ?? "",
    email: u?.email ?? "",
    idRol: u?.idRol ?? (roles.value?.[0]?.id ?? null),
    estado: isActive(u) ? "Activo" : "Inactivo",
  });

  isOpen.value = true;
}

function closeModal() {
  isOpen.value = false;
}

/** ===== VALIDATION ===== */
function validate() {
  if (!form.nombreCompleto) return "Nombre Completo es obligatorio.";
  if (!form.email) return "Email es obligatorio.";
  if (!form.idRol) return "Debes seleccionar un Rol.";
  // validación simple email
  if (!/^\S+@\S+\.\S+$/.test(form.email)) return "Email inválido.";
  return "";
}

async function readApiError(res) {
  let msg = `Error (${res.status}).`;
  try {
    const data = await res.json();
    msg = data.message || data.msg || data.error || JSON.stringify(data);
  } catch {}
  return new Error(msg);
}

/** ===== API ACTIONS ===== */
function splitNombre(nombreCompleto) {
  const parts = String(nombreCompleto ?? "").trim().split(/\s+/);
  const nombres = parts.slice(0, Math.max(1, parts.length - 1)).join(" ");
  const apellidos = parts.length > 1 ? parts[parts.length - 1] : "";
  return { nombres, apellidos };
}

async function saveUser() {
  apiError.value = "";
  const err = validate();
  if (err) {
    apiError.value = err;
    return;
  }

  saving.value = true;
  try {
    const { nombres, apellidos } = splitNombre(form.nombreCompleto);

    // Payload “flexible” (manda campos comunes para que tu API tome los que use)
    const payload = {
      idRol: Number(form.idRol),
      estado: form.estado,              // si tu API usa string
      activo: form.estado === "Activo", // si tu API usa boolean
      email: form.email,
      correo: form.email,

      nombreCompleto: form.nombreCompleto,
      nombre: form.nombreCompleto,
      nombres,
      apellidos,
    };

    // CREATE
    if (mode.value === "create") {
      const res = await fetch(USERS_ENDPOINT, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });
      if (!res.ok) throw await readApiError(res);

      let created = null;
      try { created = await res.json(); } catch { created = payload; }

      rows.value.unshift(normalizeUser(created ?? payload));
      closeModal();
      return;
    }

    // EDIT
    if (!editingId.value) {
      apiError.value = "No se encontró idUsuario para editar.";
      return;
    }

    const res = await fetch(`${USERS_ENDPOINT}/${encodeURIComponent(editingId.value)}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    if (!res.ok) throw await readApiError(res);

    let updated = null;
    try { updated = await res.json(); } catch { updated = { ...payload, idUsuario: editingId.value }; }

    const normalized = normalizeUser(updated ?? payload);
    const idx = rows.value.findIndex((x) => (x?.id ?? null) === editingId.value);
    if (idx !== -1) rows.value[idx] = { ...rows.value[idx], ...normalized };

    closeModal();
  } catch (e) {
    apiError.value = e?.message ?? "Error guardando el usuario.";
  } finally {
    saving.value = false;
  }
}

async function removeUser(u) {
  const id = u?.id ?? null;
  const name = displayName(u);

  if (!id) {
    alert("Este registro no tiene 'idUsuario'. No se puede eliminar sin id.");
    return;
  }

  const ok = confirm(`¿Seguro que deseas eliminar ${name}?`);
  if (!ok) return;

  try {
    const res = await fetch(`${USERS_ENDPOINT}/${encodeURIComponent(id)}`, { method: "DELETE" });
    if (!res.ok) throw await readApiError(res);

    rows.value = rows.value.filter((x) => (x?.id ?? null) !== id);
  } catch (e) {
    alert(e?.message ?? "No se pudo eliminar.");
  }
}
</script>

<style scoped>
	/* ===== Layout base (igual estilo que tus vistas) ===== */
	.page {
		min-height: 100vh;
		background: #eef3ff;
	}

	.content {
		padding: 22px;
	}

	/* ===== Header ===== */
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
		background: rgba(99,102,241,.10);
		border: 1px solid rgba(99,102,241,.16);
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

	/* ===== Search ===== */
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
		margin-bottom: 14px;
	}

	.search-ic {
		opacity: .75;
	}

	.search-in {
		border: 0;
		outline: none;
		width: 100%;
		font-size: 14px;
		background: transparent;
		color: #0f172a;
	}

	/* ===== Stats (4) ===== */
	.stats4 {
		display: grid;
		grid-template-columns: repeat(4,minmax(0,1fr));
		gap: 14px;
		margin-bottom: 16px;
	}

	.stat {
		border-radius: 14px;
		padding: 16px 18px;
		border: 1px solid rgba(15,23,42,.06);
		box-shadow: 0 12px 24px rgba(10,20,70,.06);
	}

	.stat-soft {
		background: rgba(255,255,255,.7);
	}

	.purpleBg {
		background: rgba(124,58,237,.06);
	}

	.greenBg {
		background: rgba(34,197,94,.06);
	}

	.orangeBg {
		background: rgba(249,115,22,.06);
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

	/* ===== Card/Table ===== */
	.card {
		background: rgba(255,255,255,.92);
		border: 1px solid rgba(15,23,42,.08);
		border-radius: 16px;
		box-shadow: 0 16px 30px rgba(10,20,70,.08);
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
		border-bottom: 1px solid rgba(15,23,42,.06);
		background: rgba(248,250,252,.7);
		border-radius: 12px;
	}

	.trow {
		display: grid;
		gap: 14px;
		padding: 16px 12px;
		border-bottom: 1px solid rgba(15,23,42,.05);
		align-items: center;
		font-size: 13px;
	}

	.usersHead {
		grid-template-columns: 2fr 2fr 1.2fr 1.1fr 1.2fr .7fr;
	}

	.usersRow {
		grid-template-columns: 2fr 2fr 1.2fr 1.1fr 1.2fr .7fr;
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
		border: 1px solid rgba(15,23,42,.10);
		background: rgba(255,255,255,.95);
		cursor: pointer;
		display: grid;
		place-items: center;
		box-shadow: 0 10px 18px rgba(10,20,70,.06);
	}

		.icon-btn.edit {
			color: #2563eb;
		}

		.icon-btn.del {
			color: #ef4444;
		}

	/* ===== Usuario cell (avatar + name) ===== */
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
		background: rgba(124,58,237,.9);
	}

	.av-blue {
		background: rgba(59,130,246,.9);
	}

	.uInfo {
		min-width: 0;
	}

	.uName {
		font-weight: 900;
		color: #0f172a;
	}

	/* ===== Chips ===== */
	.chip {
		display: inline-flex;
		align-items: center;
		gap: 8px;
		padding: 7px 10px;
		border-radius: 999px;
		font-weight: 900;
		font-size: 12px;
		border: 1px solid rgba(15,23,42,.06);
	}

		.chip .dot {
			width: 8px;
			height: 8px;
			border-radius: 999px;
			display: inline-block;
		}

	.chip-admin {
		background: rgba(124,58,237,.10);
		color: #7c3aed;
	}

		.chip-admin .dot {
			background: #7c3aed;
		}

	.chip-user {
		background: rgba(59,130,246,.10);
		color: #2563eb;
	}

		.chip-user .dot {
			background: #2563eb;
		}

	.chip-auditor {
		background: rgba(249,115,22,.10);
		color: #f97316;
	}

		.chip-auditor .dot {
			background: #f97316;
		}

	/* ===== Estado ===== */
	.state {
		display: flex;
		align-items: center;
		gap: 10px;
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

	/* ===== Footer ===== */
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

	/* ===== Modal ===== */
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

	.field label {
		display: block;
		margin-bottom: 8px;
		font-weight: 800;
		color: #64748b;
	}

	.field input, .field select {
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

		.field input:focus, .field select:focus {
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

	/* ===== Responsive ===== */
	@media (max-width: 1100px) {
		.stats4 {
			grid-template-columns: 1fr;
		}

		.usersHead, .usersRow {
			grid-template-columns: 1fr;
		}

		.actions-h {
			text-align: left;
		}

		.actions {
			justify-content: flex-start;
		}
	}
</style>
