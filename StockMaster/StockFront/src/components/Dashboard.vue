<template>
    <div class="dash">
        <div class="kpis">
            <div class="kpi k1">
                <div class="kpi-ic">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M12 2 3 7l9 5 9-5-9-5Z" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
                        <path d="M3 7v10l9 5 9-5V7" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
                    </svg>
                </div>
                <div class="kpi-mid">
                    <div class="kpi-val">820</div>
                    <div class="kpi-lbl">Productos Totales</div>
                </div>
                <div class="kpi-mini">+12%</div>
            </div>

            <div class="kpi k2">
                <div class="kpi-ic">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M7 7h10v10" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                        <path d="M17 7 7 17" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                    </svg>
                </div>
                <div class="kpi-mid">
                    <div class="kpi-val">+235</div>
                    <div class="kpi-lbl">Entradas Este Mes</div>
                </div>
                <div class="kpi-mini">+12%</div>
            </div>

            <div class="kpi k3">
                <div class="kpi-ic">
                    <svg viewBox="0 0 24 24" fill="none">
                        <path d="M7 17h10V7" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                        <path d="M17 17 7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                    </svg>
                </div>
                <div class="kpi-mid">
                    <div class="kpi-val">-182</div>
                    <div class="kpi-lbl">Salidas Este Mes</div>
                </div>
                <div class="kpi-mini">+8%</div>
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
                    <div class="kpi-val">15</div>
                    <div class="kpi-lbl">Productos con Bajo Stock</div>
                </div>
            </div>
        </div>

        <div class="card chart">
            <div class="card-head">
                <div class="h">Resumen General del Inventario</div>
                <button class="dd">
                    Últimos 6 meses
                    <span class="chev">⌄</span>
                </button>
            </div>

            <div class="chart-body">
                <div class="yaxis">
                    <span>400</span><span>350</span><span>200</span><span>100</span>
                </div>

                <div class="plot">
                    <svg class="svg" viewBox="0 0 920 230" preserveAspectRatio="none">
                        <g class="grid">
                            <line v-for="y in 5" :key="'gy'+y" :x1="0" :x2="920" :y1="y*38" :y2="y*38" />
                        </g>

                        <!-- green -->
                        <polyline :points="toPoints(entradas)" class="line g" />
                        <g>
                            <circle v-for="(p,i) in entradasPts" :key="'eg'+i" :cx="p.x" :cy="p.y" r="5" class="dot dg" />
                        </g>

                        <polyline :points="toPoints(salidas)" class="line b" />
                        <g>
                            <circle v-for="(p,i) in salidasPts" :key="'eb'+i" :cx="p.x" :cy="p.y" r="5" class="dot db" />
                        </g>
                    </svg>

                    <div class="tip">
                        <div class="tip-top">+87 <span>Entradas</span></div>
                        <div class="tip-sub">Abril, 2024</div>
                    </div>

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

        <div class="bottom">
            <div class="card">
                <div class="card-head">
                    <div class="h">Productos con Stock Bajo</div>
                    <button class="dd">
                        Ver Todos <span class="chev">⌄</span>
                    </button>
                </div>

                <div class="table">
                    <div class="thead">
                        <div>Producto</div><div>Categoría</div><div class="r">Stock</div><div class="r">Stock Minimo</div>
                    </div>

                    <div class="row" v-for="p in lowStock" :key="p.codigo">
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

                    <div class="foot">
                        <div class="foot-left">
                            <span class="ok">✓</span>
                            <span>Mostrando 5 de 15 productos con stock bajo</span>
                        </div>
                        <button class="btn">Ver Todos</button>
                    </div>
                </div>
            </div>

            <div class="card">
                <div class="card-head">
                    <div class="h">Últimos Movimientos</div>
                    <button class="dd">
                        Ver Historial <span class="chev">⌄</span>
                    </button>
                </div>

                <div class="table">
                    <div class="thead mov">
                        <div>Fecha</div><div>Tipo</div><div>Producto</div><div>Motivo</div><div>Responsible</div>
                    </div>

                    <div class="row mov" v-for="m in movimientos" :key="m.id">
                        <div class="mut">{{ m.fecha }}</div>
                        <div>
                            <span class="pill" :class="m.tipo==='Entrada' ? 'in' : 'out'">{{ m.tipo }}</span>
                        </div>
                        <div class="mut">- {{ m.producto }}</div>
                        <div class="mut">{{ m.motivo }}</div>
                        <div class="mut">{{ m.responsable }}</div>
                    </div>

                    <div class="foot">
                        <div class="foot-left">
                            <span class="clock">🕒</span>
                            <span>Últimos 8 movimientos registrados</span>
                        </div>
                        <button class="btn">Ver Historial</button>
                    </div>
                </div>
            </div>
        </div>

    </div>
</template>

<script setup>
    import { computed } from "vue";

    const months = ["Ene", "Feb", "Mar", "Abr", "May", "Jun"];

    const entradas = [120, 250, 180, 240, 160, 210];
    const salidas = [110, 200, 160, 230, 180, 260];

    const W = 920, H = 230, PAD_TOP = 18, PAD_BOTTOM = 38, PAD_LR = 34;
    const maxY = Math.max(...entradas, ...salidas) * 1.15;

    function mapX(i, n) {
        const innerW = W - PAD_LR * 2;
        return PAD_LR + (innerW * i) / (n - 1);
    }
    function mapY(v) {
        const innerH = H - PAD_TOP - PAD_BOTTOM;
        return PAD_TOP + innerH * (1 - v / maxY);
    }
    function toPoints(arr) {
        return arr.map((v, i) => `${mapX(i, arr.length)},${mapY(v)}`).join(" ");
    }

    const entradasPts = computed(() => entradas.map((v, i) => ({ x: mapX(i, entradas.length), y: mapY(v) })));
    const salidasPts = computed(() => salidas.map((v, i) => ({ x: mapX(i, salidas.length), y: mapY(v) })));

    const lowStock = [
        { codigo: "TEC-001", nombre: "Teclado Inalámbrico", categoria: "Accesorios de Computo", stock: 23, minimo: 27 },
        { codigo: "MOU-002", nombre: "Mouse Óptico Logitech M170", categoria: "Accesorios de Computo", stock: 20, minimo: 20 },
        { codigo: "MAR-003", nombre: "Martillo de Acero de 16 oz", categoria: "Herramientas", stock: 21, minimo: 23 },
        { codigo: "TOR-010", nombre: "Tornillos de Acero 5x50mm (100 uds)", categoria: "Tornillería", stock: 20, minimo: 10 },
    ];

    const movimientos = [
        { id: 1, fecha: "25 abr 2024", tipo: "Entrada", producto: "Tornillos de Acero 5x50mm (100 uds)", motivo: "Reposición", responsable: "Jose Martinez" },
        { id: 2, fecha: "24 abr 2024", tipo: "Entrada", producto: "Martillo de Acero de 16 oz", motivo: "Ajuste de Inventario", responsable: "Ana López" },
        { id: 3, fecha: "23 abr 2024", tipo: "Salida", producto: "Teclado Inalámbrico", motivo: "Devolución", responsable: "Carlos Gómez" },
        { id: 4, fecha: "22 abr 2024", tipo: "Salida", producto: "Taladro Percutor Bosch GSB 13 RE", motivo: "Venta", responsable: "María Torres" },
        { id: 5, fecha: "22 abr 2024", tipo: "Entrada", producto: "Mouse Óptico Logitech M170", motivo: "Compra", responsable: "Admin" },
    ];
</script>

<style scoped>
    .dash {
        display: flex;
        flex-direction: column;
        gap: 14px;
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

    .kpi-mini {
        margin-left: auto;
        z-index: 1;
        font-weight: 900;
        font-size: 12px;
        opacity: .9;
        align-self: flex-end;
        padding: 6px 10px;
        border-radius: 999px;
        background: rgba(255,255,255,.16);
        border: 1px solid rgba(255,255,255,.18);
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

    .tip {
        position: absolute;
        right: 170px;
        top: 62px;
        background: #fff;
        border: 1px solid rgba(15,23,42,.10);
        border-radius: 12px;
        padding: 10px 12px;
        box-shadow: 0 16px 28px rgba(0,0,0,.10);
    }

    .tip-top {
        font-weight: 1000;
        color: #0f172a;
        font-size: 18px;
    }

        .tip-top span {
            color: #35c38a;
            font-weight: 1000;
            margin-left: 6px;
            font-size: 16px;
        }

    .tip-sub {
        margin-top: 2px;
        font-weight: 900;
        color: #94a3b8;
        font-size: 12px;
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

        .tip {
            right: 16px;
            top: 12px;
        }
    }
</style>
