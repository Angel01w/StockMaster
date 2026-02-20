<template>
    <div class="dash">
        <!-- ERROR -->
        <div v-if="error" class="apiErr">
            {{ error }}
            <button class="retry" type="button" @click="loadAll" :disabled="loading">Reintentar</button>
        </div>

        <!-- KPIs -->
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

        <!-- Chart -->
        <div class="card chart">
            <div class="card-head">
                <div class="h">Resumen General del Inventario</div>
                <button class="dd" type="button" @click="toggleRange">
                    Últimos {{ rangeMonths }} meses
                    <span class="chev">⌄</span>
                </button>
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

                        <!-- green -->
                        <polyline :points="toPoints(entradasSeries)" class="line g" />
                        <g>
                            <circle v-for="(p,i) in entradasPts" :key="'eg'+i" :cx="p.x" :cy="p.y" r="5" class="dot dg" />
                        </g>

                        <!-- blue -->
                        <polyline :points="toPoints(salidasSeries)" class="line b" />
                        <g>
                            <circle v-for="(p,i) in salidasPts" :key="'eb'+i" :cx="p.x" :cy="p.y" r="5" class="dot db" />
                        </g>
                    </svg>

                    <div class="legend">
                        <span class="lg"><i class="sw g"></i>Entradas</span>
                        <span class="lg"><i class="sw b"></i>Salidas</span>
                    </div>

                    <div class="months">
                        <span v-for="m in months" :key="m">{{ m }}</span>
                    </div>
                </div>
            </div>
        </div>

        <!-- Bottom -->
        <div class="bottom">
            <div class="card">
                <div class="card-head">
                    <div class="h">Productos con Stock Bajo</div>
                    <button class="dd" type="button" @click="goLowStock">
                        Ver Todos <span class="chev">⌄</span>
                    </button>
                </div>

                <div class="table">
                    <div class="thead">
                        <div>Producto</div><div>Categoría</div><div class="r">Stock</div><div class="r">Stock Minimo</div>
                    </div>

                    <div v-if="loading" class="mutedLine">Cargando...</div>
                    <div v-else-if="lowStockRows.length === 0" class="mutedLine">No hay productos en bajo stock.</div>

                    <div class="row" v-for="p in lowStockRows" :key="p.id">
                        <div class="prod">
                            <div class="pimg" />
                            <div class="pn">
                                <div class="pname">{{ p.nombre }}</div>
                            </div>
                        </div>
                        <div class="mut">{{ p.categoria }}</div>
                        <div class="r strong">{{ p.stock }}</div>
                        <div class="r strong">{{ p.minimo }}</div>
                    </div>

                    <div class="foot" v-if="lowStockTotal > 0">
                        <div class="foot-left">
                            <span class="ok">✓</span>
                            <span>Mostrando {{ lowStockRows.length }} de {{ lowStockTotal }} productos con stock bajo</span>
                        </div>
                        <button class="btn" type="button" @click="goLowStock">Ver Todos</button>
                    </div>
                </div>
            </div>

            <div class="card">
                <div class="card-head">
                    <div class="h">Últimos Movimientos</div>
                    <button class="dd" type="button" @click="goMovimientos">
                        Ver Historial <span class="chev">⌄</span>
                    </button>
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
                        <div class="mut">- {{ m.producto }}</div>
                        <div class="mut">{{ m.motivo }}</div>
                        <div class="mut">{{ m.responsable }}</div>
                    </div>

                    <div class="foot" v-if="movRows.length > 0">
                        <div class="foot-left">
                            <span class="clock">🕒</span>
                            <span>Últimos {{ movRows.length }} movimientos registrados</span>
                        </div>
                        <button class="btn" type="button" @click="goMovimientos">Ver Historial</button>
                    </div>
                </div>
            </div>
        </div>

    </div>
</template>

<script setup>
    import { computed, onBeforeUnmount, onMounted, ref } from "vue";

    /** =========================
     *  API (AJUSTA SI CAMBIA)
     *  ========================= */
    const API_BASE = "https://localhost:7198";
    const PRODUCTOS_ENDPOINT = `${API_BASE}/api/Productos`;
    const MOVS_ENDPOINT = `${API_BASE}/api/Movimientos`;
    // opcional: si existe en tu backend, lo usa; si no, compone con Productos+Movimientos
    const DASH_ENDPOINT = `${API_BASE}/api/Dashboard`;

    /** =========================
     *  STATE
     *  ========================= */
    const loading = ref(false);
    const error = ref("");

    const productos = ref([]);
    const movimientosRaw = ref([]);

    const rangeMonths = ref(6); // 6 / 12
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

    /** cancelación segura */
    let alive = true;
    onBeforeUnmount(() => { alive = false; });

    onMounted(() => {
        loadAll();
    });

    /** =========================
     *  HELPERS
     *  ========================= */
    function normalizeList(data) {
        if (Array.isArray(data)) return data;
        if (Array.isArray(data?.items)) return data.items;
        if (Array.isArray(data?.data)) return data.data;
        return [];
    }

    function toNumber(v, d = 0) {
        const n = Number(v);
        return Number.isFinite(n) ? n : d;
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

    async function fetchJson(url, opts = {}) {
        const res = await fetch(url, opts);
        if (!res.ok) {
            let msg = `${res.status} ${res.statusText}`;
            try {
                const ct = res.headers.get("content-type") || "";
                if (ct.includes("application/json")) {
                    const j = await res.json();
                    msg = j?.message || j?.error || j?.title || msg;
                } else {
                    const t = await res.text();
                    if (t?.trim()) msg = `${msg}: ${t}`;
                }
            } catch { }
            throw new Error(msg);
        }

        const ct = res.headers.get("content-type") || "";
        if (ct.includes("application/json")) return await res.json();

        // si viene vacío
        const text = await res.text();
        return text ? JSON.parse(text) : null;
    }

    /** =========================
     *  NORMALIZERS (sin inventar)
     *  ========================= */
    function normalizeProducto(p, idx) {
        const id = p?.idProducto ?? p?.id ?? p?.productoId ?? p?.codigo ?? idx;
        const nombre = p?.nombre ?? p?.descripcion ?? p?.name ?? "";
        const categoria = p?.categoria ?? p?.categoriaNombre ?? p?.category ?? "";
        const stock = toNumber(p?.stock ?? p?.existencia ?? p?.cantidad ?? p?.qty, 0);
        const minimo = toNumber(p?.stockMinimo ?? p?.minimo ?? p?.minStock ?? p?.reorderLevel, 0);

        return { id, nombre, categoria, stock, minimo, _raw: p };
    }

    function normalizeMovimiento(m, idx) {
        const id = m?.idMovimiento ?? m?.id ?? m?.movimientoId ?? idx;

        const rawDate = m?.fecha ?? m?.createdAt ?? m?.fechaMovimiento ?? m?.date ?? null;
        const dt = rawDate ? new Date(rawDate) : null;
        const fecha =
            dt && !isNaN(dt.getTime())
                ? dt.toLocaleDateString("es-DO", { day: "2-digit", month: "short", year: "numeric" })
                : "-";

        const tipoRaw = String(m?.tipo ?? m?.tipoMovimiento ?? m?.movementType ?? "").toLowerCase();
        const tipo = tipoRaw.includes("entr") || tipoRaw === "in" ? "Entrada" : "Salida";

        const producto = m?.producto ?? m?.productoNombre ?? m?.nombreProducto ?? m?.item ?? "";
        const motivo = m?.motivo ?? m?.comentario ?? m?.reason ?? "";
        const responsable = m?.responsable ?? m?.usuario ?? m?.user ?? m?.createdBy ?? "";

        const cantidad = toNumber(m?.cantidad ?? m?.qty ?? m?.cantidadMovimiento, 0);

        return { id, fecha, tipo, producto, motivo, responsable, cantidad, _dt: dt, _raw: m };
    }

    /** =========================
     *  LOAD
     *  ========================= */
    async function loadAll() {
        loading.value = true;
        error.value = "";

        try {
            // 1) Si existe dashboard en backend, úsalo
            let dash = null;
            try {
                dash = await fetchJson(`${DASH_ENDPOINT}?months=${rangeMonths.value}`);
            } catch {
                dash = null;
            }

            if (!alive) return;

            if (dash) {
                const k = dash?.kpis ?? {};
                kpis.value = {
                    productosTotales: toNumber(k.productosTotales),
                    entradasMes: toNumber(k.entradasMes),
                    salidasMes: toNumber(k.salidasMes),
                    lowStockCount: toNumber(k.lowStockCount),
                };

                const ch = dash?.chart ?? {};
                months.value = Array.isArray(ch.months) ? ch.months : [];
                entradasSeries.value = Array.isArray(ch.entradas) ? ch.entradas.map((x) => toNumber(x)) : [];
                salidasSeries.value = Array.isArray(ch.salidas) ? ch.salidas.map((x) => toNumber(x)) : [];

                const ls = dash?.lowStock ?? {};
                const lsItems = normalizeList(ls.items);
                lowStockTotal.value = toNumber(ls.total, lsItems.length);
                lowStockRows.value = lsItems.map((p, i) => normalizeProducto(p, i))
                    .filter((p) => p.nombre) // sin inventar
                    .slice(0, 5);

                const mv = dash?.movimientos ?? {};
                movRows.value = normalizeList(mv.items).map((m, i) => normalizeMovimiento(m, i))
                    .filter((m) => m.producto || m.motivo || m.responsable || m._dt) // sin inventar
                    .slice(0, 8);

                return;
            }

            // 2) Sin /api/Dashboard: compone con Productos + Movimientos
            const [prodsRaw, movsRaw] = await Promise.all([
                fetchJson(PRODUCTOS_ENDPOINT),
                fetchJson(MOVS_ENDPOINT),
            ]);

            if (!alive) return;

            productos.value = normalizeList(prodsRaw).map((p, i) => normalizeProducto(p, i));
            movimientosRaw.value = normalizeList(movsRaw).map((m, i) => normalizeMovimiento(m, i));

            computeKPIs();
            computeLowStock();
            computeChartFromMovs();
            computeLastMovs();

        } catch (e) {
            if (!alive) return;
            error.value = e?.message ?? "Error cargando dashboard.";
        } finally {
            if (alive) loading.value = false;
        }
    }

    /** =========================
     *  COMPUTES (sin fallback)
     *  ========================= */
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

        const lowCount = productos.value
            .filter((p) => p.minimo > 0 && p.stock <= p.minimo)
            .length;

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

        const bucketsIn = Object.fromEntries(keys.map((k) => [k, 0]));
        const bucketsOut = Object.fromEntries(keys.map((k) => [k, 0]));

        for (const mv of movimientosRaw.value) {
            if (!mv._dt || isNaN(mv._dt.getTime())) continue;
            const k = monthKey(new Date(mv._dt.getFullYear(), mv._dt.getMonth(), 1));
            if (!(k in bucketsIn)) continue;

            if (mv.tipo === "Entrada") bucketsIn[k] += toNumber(mv.cantidad, 0);
            else bucketsOut[k] += toNumber(mv.cantidad, 0);
        }

        months.value = labels;
        entradasSeries.value = keys.map((k) => bucketsIn[k]);
        salidasSeries.value = keys.map((k) => bucketsOut[k]);
    }

    function computeLastMovs() {
        movRows.value = [...movimientosRaw.value]
            .sort((a, b) => {
                const ta = a._dt ? a._dt.getTime() : 0;
                const tb = b._dt ? b._dt.getTime() : 0;
                return tb - ta;
            })
            .slice(0, 8);
    }

    /** =========================
     *  CHART GEOMETRY (reactivo)
     *  ========================= */
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

    const entradasPts = computed(() =>
        entradasSeries.value.map((v, i) => ({ x: mapX(i, entradasSeries.value.length), y: mapY(v) }))
    );
    const salidasPts = computed(() =>
        salidasSeries.value.map((v, i) => ({ x: mapX(i, salidasSeries.value.length), y: mapY(v) }))
    );

    const yTicks = computed(() => {
        const m = Math.ceil(maxY.value);
        const t1 = Math.ceil(m * 1.0);
        const t2 = Math.ceil(m * 0.75);
        const t3 = Math.ceil(m * 0.5);
        const t4 = Math.ceil(m * 0.25);
        return [t1, t2, t3, t4];
    });

    /** =========================
     *  UI
     *  ========================= */
    function fmtSigned(n) {
        const v = toNumber(n, 0);
        const sign = v > 0 ? "+" : "";
        return `${sign}${v}`;
    }

    function toggleRange() {
        rangeMonths.value = rangeMonths.value === 6 ? 12 : 6;
        // si ya tenemos datos, recomputa, si no, recarga
        if (movimientosRaw.value.length > 0) computeChartFromMovs();
        else loadAll();
    }

    function goLowStock() {
        // aquí pon tu router si quieres
        // router.push("/productos?lowStock=1")
        console.log("Ir a low stock");
    }
    function goMovimientos() {
        // router.push("/movimientos")
        console.log("Ir a movimientos");
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
        grid-template-columns: repeat(4, minmax(0,1fr));
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

    .legend {
        position: absolute;
        left: 50%;
        transform: translateX(-50%);
        bottom: 38px;
        display: flex;
        gap: 22px;
        color: #64748b;
        font-weight: 900;
    }

    .lg {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 13px;
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

    .pimg {
        width: 44px;
        height: 28px;
        border-radius: 8px;
        background: linear-gradient(180deg, rgba(15,23,42,.10), rgba(15,23,42,.03));
        border: 1px solid rgba(15,23,42,.10);
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

    .btn {
        padding: 8px 12px;
        border-radius: 12px;
        border: 1px solid rgba(37,99,235,.18);
        background: rgba(37,99,235,.08);
        color: #1d4ed8;
        font-weight: 1000;
        cursor: pointer;
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

    @media (max-width: 1100px) {
        .kpis {
            grid-template-columns: repeat(2, minmax(0,1fr));
        }

        .bottom {
            grid-template-columns: 1fr;
        }
    }
</style>