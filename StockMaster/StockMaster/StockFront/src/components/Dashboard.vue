<template>
    <div class="dash">
        <div v-if="error" class="apiErr">
            {{ error }}
            <button class="retry" type="button" @click="loadAll" :disabled="loading">Reintentar</button>
        </div>

        <div class="kpis">
            <div class="kpi k1">
                <div class="kpi-ic">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M12 2 3 7l9 5 9-5-9-5Z" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
                        <path d="M3 7v10l9 5 9-5V7" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
                    </svg>
                </div>
                <div class="kpi-mid">
                    <div class="kpi-val">{{ kpis.productosTotales }}</div>
                    <div class="kpi-lbl">Productos Totales</div>
                </div>
            </div>

            <div class="kpi k2">
                <div class="kpi-ic">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M7 7h10v10" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                        <path d="M17 7 7 17" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                    </svg>
                </div>
                <div class="kpi-mid">
                    <div class="kpi-val">{{ fmtSigned(kpis.entradasMes) }}</div>
                    <div class="kpi-lbl">Entradas Este Mes</div>
                </div>
            </div>

            <div class="kpi k3">
                <div class="kpi-ic">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M7 17h10V7" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                        <path d="M17 17 7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                    </svg>
                </div>
                <div class="kpi-mid">
                    <div class="kpi-val">{{ fmtSigned(kpis.salidasMes) }}</div>
                    <div class="kpi-lbl">Salidas Este Mes</div>
                </div>
            </div>

            <div class="kpi k4">
                <div class="kpi-ic">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M12 3 2.7 20h18.6L12 3Z" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
                        <path d="M12 9v5" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                        <path d="M12 17.5h.01" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
                    </svg>
                </div>
                <div class="kpi-mid">
                    <div class="kpi-val">{{ kpis.lowStockCount }}</div>
                    <div class="kpi-lbl">Productos con Bajo Stock</div>
                </div>
            </div>
        </div>

        <div class="card chart">
            <div class="card-head">
                <div class="h">Resumen General del Inventario</div>

                <div class="head-mid">
                    <div class="legendTop">
                        <span class="lg"><i class="sw g"></i><span class="lg-txt">Entradas</span></span>
                        <span class="lg"><i class="sw b"></i><span class="lg-txt">Salidas</span></span>
                    </div>
                </div>

                <div style="display:flex; gap:10px; align-items:center;">
                    <button class="dd" type="button" @click="toggleRange">
                        Últimos {{ rangeMonths }} meses
                        <span class="chev">⌄</span>
                    </button>
                </div>
            </div>

            <div class="chart-body">
                <div class="yaxis">
                    <span v-for="(t,i) in yTicks" :key="i">{{ t }}</span>
                </div>

                <div class="plot">
                    <svg class="svg" viewBox="0 0 920 230" preserveAspectRatio="none">
                        <g class="grid">
                            <line v-for="y in 5" :key="'gy'+y" :x1="0" :x2="920" :y1="y*38" :y2="y*38" />
                        </g>

                        <polyline :points="toPoints(entradasSeries)" class="line g" />
                        <g>
                            <circle v-for="(p,i) in entradasPts" :key="'eg'+i" :cx="p.x" :cy="p.y" r="5" class="dot dg" />
                        </g>

                        <polyline :points="toPoints(salidasSeries)" class="line b" />
                        <g>
                            <circle v-for="(p,i) in salidasPts" :key="'eb'+i" :cx="p.x" :cy="p.y" r="5" class="dot db" />
                        </g>
                    </svg>

                    <div class="months">
                        <span v-for="m in months" :key="m">{{ m }}</span>
                    </div>
                </div>
            </div>
        </div>

        <div class="bottom">
            <div class="card">
                <div class="card-head">
                    <div class="h">Productos con Stock Bajo</div>
                </div>

                <div class="table">
                    <div class="thead">
                        <div>Producto</div><div>Categoría</div><div class="r">Stock</div><div class="r">Stock Minimo</div>
                    </div>

                    <div v-if="loading" class="mutedLine">Cargando...</div>
                    <div v-else-if="lowStockRows.length === 0" class="mutedLine">No hay productos en bajo stock.</div>

                    <div class="row" v-for="p in lowStockRows" :key="p.id">
                        <div class="prod">
                            <div class="pn">
                                <div class="pname">{{ p.nombre }}</div>
                            </div>
                        </div>

                        <div class="mut">{{ p.categoriaNombre }}</div>

                        <div class="r strong">{{ p.stock }}</div>
                        <div class="r strong">{{ p.minimo }}</div>
                    </div>

                    <div class="foot" v-if="lowStockTotal > 0">
                        <div class="foot-left">
                            <span class="ok">✓</span>
                            <span>Mostrando {{ lowStockRows.length }} de {{ lowStockTotal }} productos con stock bajo</span>
                        </div>
                    </div>
                </div>
            </div>

            <div class="card">
                <div class="card-head">
                    <div class="h">Últimos Movimientos</div>
                </div>

                <div class="table">
                    <div class="thead mov">
                        <div>Fecha</div><div>Tipo</div><div>Producto</div><div>Motivo</div><div>Responsible</div>
                    </div>

                    <div v-if="loading" class="mutedLine">Cargando...</div>
                    <div v-else-if="movRows.length === 0" class="mutedLine">No hay movimientos registrados.</div>

                    <div class="row mov" v-for="m in movRows" :key="m.id">
                        <div class="mut">{{ m.fecha }}</div>
                        <div>
                            <span class="pill" :class="m.tipo==='Entrada' ? 'in' : 'out'">{{ m.tipo }}</span>
                        </div>
                        <div class="mut">{{ m.producto }}</div>
                        <div class="mut">{{ m.motivo }}</div>
                        <div class="mut">{{ m.responsable }}</div>
                    </div>

                    <div class="foot" v-if="movRows.length > 0">
                        <div class="foot-left">
                            <span class="clock">🕒</span>
                            <span>Últimos {{ movRows.length }} movimientos registrados</span>
                        </div>
                    </div>
                </div>
            </div>
        </div>

    </div>
</template>

<script setup>
    import { computed, onBeforeUnmount, onMounted, ref } from "vue";
    import { apiFetch } from "../services/api";

    const API_BASE = "https://localhost:7198";
	const PRODUCTOS_ENDPOINT = "/api/Productos";
	const MOVS_ENDPOINT = "/api/Movimientos";

    const USUARIOS_ENDPOINTS = [
        `${API_BASE}/api/Usuarios`,
        `${API_BASE}/api/Usuario`,
        `${API_BASE}/api/Users`,
        `${API_BASE}/api/UsuariosSistema`,
    ];

    const MOTIVOS_ENDPOINTS = [
        `${API_BASE}/api/MotivosMovimiento`,
        `${API_BASE}/api/Motivos`,
        `${API_BASE}/api/Motivo`,
        `${API_BASE}/api/MotivoMovimiento`,
    ];

    const loading = ref(false);
    const error = ref("");

    const productos = ref([]);
    const usuarios = ref([]);
    const motivos = ref([]);
    const movimientosRaw = ref([]);

    const rangeMonths = ref(6);
    const months = ref([]);
    const entradasSeries = ref([]);
    const salidasSeries = ref([]);

    const lowStockRows = ref([]);
    const lowStockTotal = ref(0);

    const movRows = ref([]);

    const kpis = ref({
        productosTotales: 0,
        entradasMes: 0,
        salidasMes: 0,
        lowStockCount: 0,
    });

    let alive = true;
    onBeforeUnmount(() => {
        alive = false;
    });
    onMounted(() => loadAll());

    function normalizeList(data) {
        if (Array.isArray(data)) return data;
        if (data && Array.isArray(data.$values)) return data.$values;
        if (data && Array.isArray(data.items)) return data.items;
        if (data && Array.isArray(data.data)) return data.data;
        if (data && Array.isArray(data.result)) return data.result;
        if (data && Array.isArray(data.value)) return data.value;
        if (data && Array.isArray(data.results)) return data.results;
        return [];
    }

    function toNumber(v, d) {
        const n = Number(v);
        return Number.isFinite(n) ? n : (d === undefined ? 0 : d);
    }

    function monthKey(dt) {
        const y = dt.getFullYear();
        const m = dt.getMonth() + 1;
        return `${y}-${String(m).padStart(2, "0")}`;
    }

    function monthLabel(dt) {
        const map = ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];
        return map[dt.getMonth()];
    }

    async function fetchFirstList(endpoints) {
        let lastErr = null;

        for (const url of endpoints) {
            try {
                const data = await apiFetch(url);
                return { url, list: normalizeList(data) };
            } catch (e) {
                lastErr = e;
            }
        }

        return { url: endpoints[0], list: [], error: (lastErr && lastErr.message) ? lastErr.message : "No se pudo cargar lista." };
    }

    function extractCategoriaNombre(raw) {
        const c = raw && (raw.categoria || raw.Categoria || raw.category || raw.categoriaDto) ? (raw.categoria || raw.Categoria || raw.category || raw.categoriaDto) : null;

        if (c && typeof c === "object") {
            const name = c.nombre || c.name || c.descripcion || c.description || "";
            return String(name || "—").trim();
        }

        const asStr = raw ? (raw.categoriaNombre || raw.categoryName || c) : null;
        if (asStr && String(asStr).trim()) return String(asStr).trim();

        const id = raw ? (raw.idCategoria || raw.categoriaId || raw.IdCategoria) : null;
        return id ? `ID ${id}` : "—";
    }

    function normalizeProducto(p, idx) {
        const id = (p && (p.idProducto || p.IdProducto || p.id || p.Id || p.productoId)) ? (p.idProducto || p.IdProducto || p.id || p.Id || p.productoId) : idx;
        const nombre = (p && (p.nombre || p.Nombre || p.descripcion || p.Descripcion || p.name)) ? (p.nombre || p.Nombre || p.descripcion || p.Descripcion || p.name) : "";
        const stock = toNumber(p && (p.stockActual ?? p.StockActual ?? p.stock ?? p.existencia ?? p.cantidad ?? p.qty), 0);
        const minimo = toNumber(p && (p.stockMinimo ?? p.StockMinimo ?? p.minimo ?? p.minStock ?? p.reorderLevel), 0);
        const categoriaNombre = extractCategoriaNombre(p);

        return { id: Number(id), nombre: String(nombre || "").trim(), categoriaNombre, stock, minimo, _raw: p };
    }

    function normalizeUsuario(u, idx) {
        const id = (u && (u.idUsuario || u.IdUsuario || u.id || u.Id)) ? (u.idUsuario || u.IdUsuario || u.id || u.Id) : idx;

        const nombreCompleto =
            (u && (u.nombreCompleto || u.NombreCompleto || u.nombre || u.Nombre || u.username || u.Username || u.email || u.Email))
                ? (u.nombreCompleto || u.NombreCompleto || u.nombre || u.Nombre || u.username || u.Username || u.email || u.Email)
                : "";

        return { id: Number(id), nombre: String(nombreCompleto || "").trim(), _raw: u };
    }

    function normalizeMotivo(mm, idx) {
        const id =
            (mm && (mm.idMotivo || mm.IdMotivo || mm.id || mm.Id || mm.idMotivoMovimiento || mm.IdMotivoMovimiento))
                ? (mm.idMotivo || mm.IdMotivo || mm.id || mm.Id || mm.idMotivoMovimiento || mm.IdMotivoMovimiento)
                : idx;

        const nombre = (mm && (mm.nombre || mm.Nombre || mm.descripcion || mm.Descripcion)) ? (mm.nombre || mm.Nombre || mm.descripcion || mm.Descripcion) : "";
        return { id: Number(id), nombre: String(nombre || "").trim(), _raw: mm };
    }

    function normalizeMovimiento(m, idx) {
        const id = (m && (m.idMovimiento || m.IdMovimiento || m.id || m.movimientoId)) ? (m.idMovimiento || m.IdMovimiento || m.id || m.movimientoId) : idx;

        const rawDate = m ? (m.fecha || m.Fecha || m.createdAt || m.CreatedAt || m.fechaMovimiento || m.date) : null;
        const dt = rawDate ? new Date(rawDate) : null;
        const fecha =
            dt && !isNaN(dt.getTime())
                ? dt.toLocaleDateString("es-DO", { day: "2-digit", month: "short", year: "numeric" })
                : "-";

        let tipo = m ? (m.tipo || m.Tipo || m.tipoMovimiento || m.movementType || "Entrada") : "Entrada";
        if (typeof tipo === "number") tipo = tipo === 1 ? "Entrada" : "Salida";
        if (typeof tipo === "string") {
            const t = tipo.toLowerCase();
            if (t.indexOf("e") === 0) tipo = "Entrada";
            else if (t.indexOf("s") === 0) tipo = "Salida";
            else if (t === "entrada" || t === "salida") tipo = t[0].toUpperCase() + t.slice(1);
        }

        const idProducto = Number(m ? (m.idProducto || m.IdProducto || m.productoId || 0) : 0) || null;
        const idUsuario = Number(m ? (m.idUsuario || m.IdUsuario || m.usuarioId || 0) : 0) || null;

        const idMotivo = Number(
            m
                ? (m.idMotivo || m.IdMotivo || m.idMotivoMovimiento || m.IdMotivoMovimiento || m.motivoId || 0)
                : 0
        ) || null;

        const motivoDirecto =
            m
                ? (m.motivoNombre ||
                    m.MotivoNombre ||
                    (m.motivo && (m.motivo.nombre || m.motivo.descripcion)) ||
                    (m.Motivo && m.Motivo.Nombre) ||
                    m.motivo ||
                    m.comentario ||
                    m.reason ||
                    "")
                : "";

        return {
            id,
            fecha,
            tipo,
            idProducto,
            idUsuario,
            idMotivo,
            motivo: String(motivoDirecto || "").trim() || "",
            producto: "",
            responsable: "",
            cantidad: toNumber(m ? (m.cantidad || m.Cantidad || m.qty || 0) : 0, 0),
            _dt: dt,
            _raw: m,
        };
    }

    function resolveProductoNombre(m) {
        const r = m ? m._raw : null;
        const direct = r ? (r.productoNombre || r.ProductoNombre || (r.producto && r.producto.nombre) || (r.Producto && r.Producto.Nombre)) : null;
        if (direct && String(direct).trim() && String(direct).trim() !== "-") return String(direct).trim();

        const id = Number(m && (m.idProducto || (r && (r.idProducto || r.IdProducto))));
        if (!id) return "-";
        const found = productos.value.find((p) => Number(p.id) === id);
        return found ? found.nombre : `ID ${id}`;
    }

    function resolveUsuarioNombre(m) {
        const r = m ? m._raw : null;
        const direct = r ? (r.usuarioNombre || r.UsuarioNombre || (r.usuario && (r.usuario.nombreCompleto || r.usuario.username)) || (r.Usuario && r.Usuario.NombreCompleto)) : null;
        if (direct && String(direct).trim() && String(direct).trim() !== "-") return String(direct).trim();

        const id = Number(m && (m.idUsuario || (r && (r.idUsuario || r.IdUsuario))));
        if (!id) return "-";
        const found = usuarios.value.find((u) => Number(u.id) === id);
        return found ? found.nombre : `ID ${id}`;
    }

    function resolveMotivoNombre(m) {
        if (m && m.motivo && String(m.motivo).trim() && String(m.motivo).trim() !== "-") return String(m.motivo).trim();

        const r = m ? m._raw : null;
        const direct = r ? (r.motivoNombre || r.MotivoNombre || (r.motivo && (r.motivo.nombre || r.motivo.descripcion)) || (r.Motivo && r.Motivo.Nombre)) : null;
        if (direct && String(direct).trim()) return String(direct).trim();

        const id = Number(m && (m.idMotivo || (r && (r.idMotivo || r.IdMotivo || r.idMotivoMovimiento || r.IdMotivoMovimiento))));
        if (!id) return "-";
        const found = motivos.value.find((x) => Number(x.id) === id);
        return found ? found.nombre : `ID ${id}`;
    }

    async function loadAll() {
        if (loading.value) return;

        loading.value = true;
        error.value = "";

        try {
            const packs = await Promise.all([
                apiFetch(PRODUCTOS_ENDPOINT),
                fetchFirstList(USUARIOS_ENDPOINTS),
                fetchFirstList(MOTIVOS_ENDPOINTS),
                apiFetch(MOVS_ENDPOINT),
            ]);

            if (!alive) return;

            const prodsRaw = packs[0];
            const usersPack = packs[1];
            const motivosPack = packs[2];
            const movsRaw = packs[3];

            productos.value = normalizeList(prodsRaw).map((p, i) => normalizeProducto(p, i));
            usuarios.value = normalizeList(usersPack.list).map((u, i) => normalizeUsuario(u, i)).filter((x) => x.id && x.nombre);
            motivos.value = normalizeList(motivosPack.list).map((mm, i) => normalizeMotivo(mm, i)).filter((x) => x.id && x.nombre);

            movimientosRaw.value = normalizeList(movsRaw).map((m, i) => normalizeMovimiento(m, i));

            computeKPIs();
            computeLowStock();
            computeChartFromMovs();
            computeLastMovs();
        } catch (e) {
            if (!alive) return;
            error.value = (e && e.message) ? e.message : "Error cargando dashboard.";
        } finally {
            if (alive) loading.value = false;
        }
    }

    function computeKPIs() {
        const totalProds = productos.value.length;

        const now = new Date();
        const from = new Date(now.getFullYear(), now.getMonth(), 1);

        const inMes = movimientosRaw.value
            .filter((x) => x._dt && x._dt >= from && x.tipo === "Entrada")
            .reduce((a, x) => a + toNumber(x.cantidad, 0), 0);

        const outMes = movimientosRaw.value
            .filter((x) => x._dt && x._dt >= from && x.tipo === "Salida")
            .reduce((a, x) => a + toNumber(x.cantidad, 0), 0);

        const lowCount = productos.value.filter((p) => p.minimo > 0 && p.stock <= p.minimo).length;

        kpis.value = {
            productosTotales: totalProds,
            entradasMes: inMes,
            salidasMes: outMes,
            lowStockCount: lowCount,
        };
    }

    function computeLowStock() {
        const list = productos.value
            .filter((p) => p.minimo > 0 && p.stock <= p.minimo && p.nombre)
            .sort((a, b) => (a.stock - a.minimo) - (b.stock - b.minimo));

        lowStockTotal.value = list.length;
        lowStockRows.value = list.slice(0, 5);
    }

    function computeChartFromMovs() {
        const n = rangeMonths.value;
        const now = new Date();

        const monthDates = [];
        for (let i = n - 1; i >= 0; i--) {
            monthDates.push(new Date(now.getFullYear(), now.getMonth() - i, 1));
        }

        const keys = monthDates.map(monthKey);
        const labels = monthDates.map(monthLabel);

        const bucketsIn = {};
        const bucketsOut = {};
        for (let i = 0; i < keys.length; i++) {
            bucketsIn[keys[i]] = 0;
            bucketsOut[keys[i]] = 0;
        }

        for (const mv of movimientosRaw.value) {
            if (!mv._dt || isNaN(mv._dt.getTime())) continue;
            const k = monthKey(new Date(mv._dt.getFullYear(), mv._dt.getMonth(), 1));
            if (bucketsIn[k] === undefined) continue;

            if (mv.tipo === "Entrada") bucketsIn[k] += toNumber(mv.cantidad, 0);
            else bucketsOut[k] += toNumber(mv.cantidad, 0);
        }

        months.value = labels;
        entradasSeries.value = keys.map((k) => bucketsIn[k]);
        salidasSeries.value = keys.map((k) => bucketsOut[k]);
    }

    function computeLastMovs() {
        const list = [...movimientosRaw.value]
            .sort((a, b) => {
                const ta = a._dt ? a._dt.getTime() : 0;
                const tb = b._dt ? b._dt.getTime() : 0;
                return tb - ta;
            })
            .slice(0, 8);

        movRows.value = list.map((m) => ({
            ...m,
            producto: resolveProductoNombre(m),
            responsable: resolveUsuarioNombre(m),
            motivo: resolveMotivoNombre(m),
        }));
    }

    const W = 920, H = 230, PAD_TOP = 18, PAD_BOTTOM = 38, PAD_LR = 34;

    const maxY = computed(() => {
        const all = [...entradasSeries.value, ...salidasSeries.value].map((x) => toNumber(x, 0));
        const m = Math.max(1, ...all);
        return m * 1.15;
    });

    function mapX(i, n) {
        const innerW = W - PAD_LR * 2;
        return n <= 1 ? PAD_LR : PAD_LR + (innerW * i) / (n - 1);
    }
    function mapY(v) {
        const innerH = H - PAD_TOP - PAD_BOTTOM;
        return PAD_TOP + innerH * (1 - toNumber(v, 0) / maxY.value);
    }
    function toPoints(arr) {
        const a = Array.isArray(arr) ? arr : [];
        if (a.length === 0) return "";
        return a.map((v, i) => `${mapX(i, a.length)},${mapY(v)}`).join(" ");
    }

    const entradasPts = computed(() => entradasSeries.value.map((v, i) => ({ x: mapX(i, entradasSeries.value.length), y: mapY(v) })));
    const salidasPts = computed(() => salidasSeries.value.map((v, i) => ({ x: mapX(i, salidasSeries.value.length), y: mapY(v) })));

    const yTicks = computed(() => {
        const m = Math.ceil(maxY.value);
        return [Math.ceil(m * 1.0), Math.ceil(m * 0.75), Math.ceil(m * 0.5), Math.ceil(m * 0.25)];
    });

    function fmtSigned(n) {
        const v = toNumber(n, 0);
        const sign = v > 0 ? "+" : "";
        return `${sign}${v}`;
    }

    function toggleRange() {
        rangeMonths.value = rangeMonths.value === 6 ? 12 : 6;
        if (movimientosRaw.value.length > 0) computeChartFromMovs();
        else loadAll();
    }
</script>

<style scoped>
    .dash {
        display: flex;
        flex-direction: column;
        gap: 14px;
    }

    .apiErr {
        border: 1px solid rgba(239,68,68,.25);
        background: rgba(239,68,68,.08);
        color: #b91c1c;
        padding: 12px 14px;
        border-radius: 12px;
        font-weight: 900;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
    }

    .retry {
        padding: 8px 12px;
        border-radius: 12px;
        border: 1px solid rgba(239,68,68,.25);
        background: rgba(255,255,255,.7);
        cursor: pointer;
        font-weight: 1000;
    }

    .mutedLine {
        color: #64748b;
        font-weight: 900;
        padding: 12px 0;
    }

    .kpis {
        display: grid;
        grid-template-columns: repeat(4,minmax(0,1fr));
        gap: 14px;
    }

    .kpi {
        height: 78px;
        border-radius: 14px;
        display: flex;
        align-items: center;
        padding: 14px 16px;
        color: #fff;
        position: relative;
        overflow: hidden;
        box-shadow: 0 14px 28px rgba(0,0,0,.10);
    }

        .kpi::after {
            content: "";
            position: absolute;
            inset: -40px -60px auto auto;
            width: 170px;
            height: 170px;
            border-radius: 999px;
            background: rgba(255,255,255,.16);
            transform: rotate(12deg);
        }

    .kpi-ic {
        width: 42px;
        height: 42px;
        border-radius: 12px;
        background: rgba(255,255,255,.18);
        display: grid;
        place-items: center;
        margin-right: 12px;
        z-index: 1;
    }

        .kpi-ic svg {
            width: 22px;
            height: 22px;
            color: #fff;
            opacity: .95;
        }

    .kpi-mid {
        z-index: 1;
    }

    .kpi-val {
        font-weight: 1000;
        font-size: 24px;
        line-height: 1;
    }

    .kpi-lbl {
        margin-top: 6px;
        font-weight: 900;
        font-size: 12px;
        opacity: .95;
    }

    .k1 {
        background: linear-gradient(90deg,#2a64f3,#215ae6);
    }

    .k2 {
        background: linear-gradient(90deg,#2fb079,#49c18f);
    }

    .k3 {
        background: linear-gradient(90deg,#ff9c41,#f07b2d);
    }

    .k4 {
        background: linear-gradient(90deg,#ff6c6c,#ff4f4f);
    }

    .card {
        background: rgba(255,255,255,.72);
        border: 1px solid rgba(15,23,42,.08);
        border-radius: 14px;
        box-shadow: 0 14px 34px rgba(0,0,0,.08);
    }

    .card-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 14px 14px 10px;
    }

    .h {
        font-weight: 1000;
        color: #0f172a;
        font-size: 16px;
    }

    .head-mid {
        flex: 1;
        display: flex;
        justify-content: center;
        padding: 0 12px;
        min-width: 0;
    }

    .dd {
        border: 1px solid rgba(15,23,42,.10);
        background: rgba(255,255,255,.65);
        padding: 8px 12px;
        border-radius: 12px;
        font-weight: 900;
        color: #334155;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 10px;
    }

    .chev {
        opacity: .7;
        font-weight: 900;
    }

    .legendTop {
        display: inline-flex;
        align-items: center;
        gap: 18px;
        padding: 8px 12px;
        border-radius: 999px;
        background: rgba(255,255,255,.88);
        border: 1px solid rgba(15,23,42,.10);
        box-shadow: 0 10px 18px rgba(0,0,0,.08);
        color: #0f172a;
        font-weight: 1000;
        backdrop-filter: blur(6px);
        max-width: 100%;
    }

    .lg {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        font-size: 13px;
        white-space: nowrap;
    }

    .lg-txt {
        color: #0f172a;
        text-shadow: 0 1px 0 rgba(255,255,255,.6);
    }

    .sw {
        width: 26px;
        height: 8px;
        border-radius: 999px;
        display: inline-block;
    }

        .sw.g {
            background: #35c38a;
        }

        .sw.b {
            background: #2a64f3;
        }

    .chart-body {
        display: grid;
        grid-template-columns: 42px 1fr;
        gap: 10px;
        padding: 0 14px 14px;
    }

    .yaxis {
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        padding: 10px 0 32px;
        color: #64748b;
        font-weight: 900;
        font-size: 12px;
    }

    .plot {
        position: relative;
    }

    .svg {
        width: 100%;
        height: 240px;
        border-radius: 14px;
        background: linear-gradient(180deg, rgba(37,99,235,.08), rgba(37,99,235,0));
        border: 1px solid rgba(37,99,235,.10);
    }

    .grid line {
        stroke: rgba(15,23,42,.08);
        stroke-width: 1;
    }

    .line {
        fill: none;
        stroke-width: 3.2;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

        .line.g {
            stroke: #35c38a;
            filter: drop-shadow(0 8px 10px rgba(53,195,138,.15));
        }

        .line.b {
            stroke: #2a64f3;
            filter: drop-shadow(0 8px 10px rgba(42,100,243,.12));
        }

    .dot {
        stroke: #fff;
        stroke-width: 2.5;
    }

        .dot.dg {
            fill: #35c38a;
        }

        .dot.db {
            fill: #2a64f3;
        }

    .months {
        position: absolute;
        left: 16px;
        right: 16px;
        bottom: 12px;
        display: flex;
        justify-content: space-between;
        color: #64748b;
        font-weight: 900;
        font-size: 12px;
    }

    .bottom {
        display: grid;
        grid-template-columns: 1.1fr .9fr;
        gap: 14px;
    }

    .table {
        padding: 0 14px 12px;
    }

    .thead {
        display: grid;
        grid-template-columns: 1.4fr 1fr .55fr .75fr;
        gap: 12px;
        color: #64748b;
        font-weight: 1000;
        font-size: 13px;
        padding: 10px 0;
        border-bottom: 1px solid rgba(15,23,42,.08);
    }

    .row {
        display: grid;
        grid-template-columns: 1.4fr 1fr .55fr .75fr;
        gap: 12px;
        padding: 12px 0;
        border-bottom: 1px solid rgba(15,23,42,.06);
        align-items: center;
        font-size: 13px;
    }

    .r {
        text-align: right;
    }

    .strong {
        font-weight: 1000;
        color: #0f172a;
    }

    .mut {
        color: #475569;
        font-weight: 800;
    }

    .prod {
        display: flex;
        align-items: center;
        gap: 12px;
    }

    .pname {
        font-weight: 1000;
        color: #0f172a;
    }

    .foot {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 10px;
        padding-top: 12px;
    }

    .foot-left {
        display: flex;
        align-items: center;
        gap: 10px;
        color: #64748b;
        font-weight: 900;
        font-size: 12px;
    }

    .ok {
        width: 18px;
        height: 18px;
        border-radius: 6px;
        background: rgba(34,197,94,.16);
        color: #15803d;
        display: grid;
        place-items: center;
        font-weight: 1000;
    }

    .clock {
        opacity: .8;
    }

    .thead.mov, .row.mov {
        grid-template-columns: .9fr .7fr 1.3fr 1fr 1fr;
    }

    .pill {
        display: inline-flex;
        padding: 6px 12px;
        border-radius: 999px;
        font-weight: 1000;
        font-size: 12px;
        border: 1px solid transparent;
    }

        .pill.in {
            background: rgba(34,197,94,.16);
            color: #15803d;
            border-color: rgba(34,197,94,.25);
        }

        .pill.out {
            background: rgba(239,68,68,.16);
            color: #b91c1c;
            border-color: rgba(239,68,68,.25);
        }

    @media (max-width:1100px) {
        .kpis {
            grid-template-columns: repeat(2,minmax(0,1fr));
        }

        .bottom {
            grid-template-columns: 1fr;
        }

        .head-mid {
            justify-content: flex-end;
        }

        .legendTop {
            margin-left: auto;
        }
    }
</style>