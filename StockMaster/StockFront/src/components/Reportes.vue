<template>
	<div class="page">
		<div class="content">
			<!-- Header -->
			<div class="hdr">
				<div class="hdr-left">
					<div class="cube" aria-hidden="true">
						<svg viewBox="0 0 24 24" fill="none">
							<path d="M12 2 3 7l9 5 9-5-9-5Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
							<path d="M3 7v10l9 5 9-5V7" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
							<path d="M12 12v10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
						</svg>
					</div>
					<div class="h1">Reportes y Consultas</div>
				</div>

				<!-- (iconos del layout suelen venir del AppLayout; aquí no se repiten) -->
			</div>

			<!-- Card: Generar Reporte -->
			<div class="card big">
				<div class="card-title">Generar Reporte</div>

				<!-- ✅ GRID + FILA FECHAS CON GAP REAL -->
				<div class="gen-grid">
					<div class="field">
						<label>Tipo de Reporte</label>
						<div class="selectWrap">
							<select v-model="form.tipo">
								<option value="stock-actual">Stock Actual</option>
								<option value="movimientos-mes">Movimientos del Mes</option>
								<option value="stock-critico">Stock Crítico</option>
							</select>
							<span class="chev" aria-hidden="true">▾</span>
						</div>
					</div>

					<!-- las fechas en una fila aparte con separación garantizada -->
					<div class="datesRow">
						<div class="field">
							<label>Fecha Desde</label>
							<div class="dateWrap">
								<input type="date" v-model="form.desde" />
							</div>
						</div>

						<div class="field">
							<label>Fecha Hasta</label>
							<div class="dateWrap">
								<input type="date" v-model="form.hasta" />
							</div>
						</div>
					</div>
				</div>

				<button class="btnGen" type="button" :disabled="generating" @click="generateAndDownload">
					<span class="btnIco" aria-hidden="true">＋</span>
					{{ generating ? "Generando..." : "Generar y Descargar Reporte" }}
				</button>
			</div>

			<!-- Cards pequeños -->
			<div class="grid3">
				<div class="card mini">
					<div class="mini-ico blue">📄</div>
					<div class="mini-title">Reporte de Stock Actual</div>
					<div class="mini-sub">Consulta el inventario actual de todos los productos</div>
					<button class="link" type="button" @click="quickReport('stock-actual')">Ver Reporte →</button>
				</div>

				<div class="card mini">
					<div class="mini-ico green">📅</div>
					<div class="mini-title">Movimientos del Mes</div>
					<div class="mini-sub">Historial de entradas y salidas del mes actual</div>
					<button class="link" type="button" @click="quickReport('movimientos-mes')">Ver Reporte →</button>
				</div>

				<div class="card mini">
					<div class="mini-ico red">⏳</div>
					<div class="mini-title">Stock Crítico</div>
					<div class="mini-sub">Productos que requieren reposición inmediata</div>
					<button class="link" type="button" @click="quickReport('stock-critico')">Ver Reporte →</button>
				</div>
			</div>

			<!-- Reportes recientes -->
			<div class="card recent">
				<div class="card-title">Reportes Recientes</div>

				<div class="recent-list">
					<div class="recent-row" v-for="r in recentReports" :key="r.id">
						<div class="docIco" aria-hidden="true">📄</div>
						<div class="recent-info">
							<div class="recent-name">{{ r.nombre }}</div>
							<div class="recent-date">{{ r.fecha }}</div>
						</div>
						<button class="dl" type="button" title="Descargar" aria-label="Descargar" @click="downloadFromUrl(r.url, r.fileName)">
							⬇
						</button>
					</div>
				</div>
			</div>

			<!-- toast simple -->
			<div v-if="toast.msg" class="toast" :class="toast.kind">{{ toast.msg }}</div>
		</div>
	</div>
</template>

<script setup>
	import { reactive, ref } from "vue";

	const API_BASE = "https://localhost:7198";

	/**
	 * Ajusta esto a TU API real:
	 * - Ideal: un endpoint que devuelva PDF como file/stream:
	 *   GET /api/Reportes/pdf?tipo=stock-actual&desde=2026-02-01&hasta=2026-02-19
	 *
	 * Si todavía no existe, el front hace fallback a un PDF “demo” generado en el navegador.
	 */
	const REPORTS_PDF_ENDPOINT = `${API_BASE}/api/Reportes/pdf`;

	const generating = ref(false);

	const form = reactive({
		tipo: "stock-actual",
		desde: "",
		hasta: "",
	});

	const toast = reactive({ msg: "", kind: "ok" });
	let toastTimer = null;

	function showToast(msg, kind = "ok") {
		toast.msg = msg;
		toast.kind = kind;
		clearTimeout(toastTimer);
		toastTimer = setTimeout(() => (toast.msg = ""), 2600);
	}

	const recentReports = ref([
		{
			id: 1,
			nombre: "Reporte Stock Abril 2024",
			fecha: "25/04/2024 - 12:00 PM",
			url: "", // si tienes url real, ponla aquí
			fileName: "Reporte_Stock_Abril_2024.pdf",
		},
		{
			id: 2,
			nombre: "Movimientos Q1 2024",
			fecha: "01/01/2024 - 1:30 PM",
			url: "",
			fileName: "Movimientos_Q1_2024.pdf",
		},
		{
			id: 3,
			nombre: "Productos Stock Crítico",
			fecha: "15/04/2024 - 10:30 AM",
			url: "",
			fileName: "Productos_Stock_Critico.pdf",
		},
	]);

	function buildFileName(tipo, desde, hasta) {
		const t =
			tipo === "stock-actual"
				? "Stock_Actual"
				: tipo === "movimientos-mes"
					? "Movimientos_Mes"
					: "Stock_Critico";

		const d = (desde || "sin_desde").replaceAll("-", "");
		const h = (hasta || "sin_hasta").replaceAll("-", "");
		return `Reporte_${t}_${d}_${h}.pdf`;
	}

	async function generateAndDownload() {
		generating.value = true;
		try {
			// Intenta descargar desde la API (PDF real)
			const params = new URLSearchParams();
			params.set("tipo", form.tipo);
			if (form.desde) params.set("desde", form.desde);
			if (form.hasta) params.set("hasta", form.hasta);

			const url = `${REPORTS_PDF_ENDPOINT}?${params.toString()}`;

			const res = await fetch(url, {
				method: "GET",
				headers: {
					Accept: "application/pdf",
				},
			});

			if (res.ok) {
				const blob = await res.blob();
				const fileName = buildFileName(form.tipo, form.desde, form.hasta);
				downloadBlob(blob, fileName);

				// agrega a recientes (simulado)
				recentReports.value.unshift({
					id: Date.now(),
					nombre:
						form.tipo === "stock-actual"
							? "Reporte de Stock Actual"
							: form.tipo === "movimientos-mes"
								? "Movimientos del Mes"
								: "Stock Crítico",
					fecha: new Date().toLocaleString("es-DO"),
					url: "",
					fileName,
				});

				showToast("Reporte generado y descargado ✅", "ok");
				return;
			}

			// Fallback: si tu API no existe todavía, genera un PDF demo en el navegador
			const fileName = buildFileName(form.tipo, form.desde, form.hasta);
			const demoPdf = await generateDemoPdf({
				tipo: form.tipo,
				desde: form.desde,
				hasta: form.hasta,
			});
			downloadBlob(demoPdf, fileName);
			showToast("API no respondió PDF. Se descargó un PDF demo 🧾", "warn");
		} catch (e) {
			// último fallback: PDF demo
			const fileName = buildFileName(form.tipo, form.desde, form.hasta);
			const demoPdf = await generateDemoPdf({
				tipo: form.tipo,
				desde: form.desde,
				hasta: form.hasta,
			});
			downloadBlob(demoPdf, fileName);
			showToast("Error con la API. Se descargó un PDF demo ⚠️", "warn");
		} finally {
			generating.value = false;
		}
	}

	function quickReport(tipo) {
		form.tipo = tipo;
		generateAndDownload();
	}

	function downloadFromUrl(url, fileName) {
		if (url) {
			// descarga real si tienes URL
			const a = document.createElement("a");
			a.href = url;
			a.download = fileName || "reporte.pdf";
			document.body.appendChild(a);
			a.click();
			a.remove();
			return;
		}
		// si no hay url, genera demo
		generateAndDownload();
	}

	function downloadBlob(blob, fileName) {
		const blobUrl = URL.createObjectURL(blob);
		const a = document.createElement("a");
		a.href = blobUrl;
		a.download = fileName;
		document.body.appendChild(a);
		a.click();
		a.remove();
		setTimeout(() => URL.revokeObjectURL(blobUrl), 800);
	}

	/**
	 * PDF demo sin librerías:
	 * genera un PDF súper simple (texto básico).
	 * Si tú ya tienes backend que devuelve PDF, esto casi nunca se usará.
	 */
	async function generateDemoPdf({ tipo, desde, hasta }) {
		// PDF minimal “Hello” (muy básico) construyendo bytes.
		// Para un PDF elegante, lo ideal es backend o usar jspdf/pdf-lib.
		const title =
			tipo === "stock-actual"
				? "Reporte: Stock Actual"
				: tipo === "movimientos-mes"
					? "Reporte: Movimientos del Mes"
					: "Reporte: Stock Crítico";

		const meta = `Desde: ${desde || "—"}   Hasta: ${hasta || "—"}`;

		// PDF mínimo con 1 página, fuente Helvetica, 2 líneas
		const content = [
			"%PDF-1.4",
			"1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
			"2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj",
			"3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj",
			"4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj",
			"5 0 obj << /Length 6 0 R >> stream",
			"BT",
			"/F1 18 Tf",
			"72 720 Td",
			`(${escapePdfText(title)}) Tj`,
			"0 -28 Td",
			"/F1 12 Tf",
			`(${escapePdfText(meta)}) Tj`,
			"ET",
			"endstream endobj",
			"6 0 obj 999 endobj", // placeholder length (no estrictamente correcto, pero la mayoría abre)
			"xref",
			"0 7",
			"0000000000 65535 f ",
			"trailer << /Size 7 /Root 1 0 R >>",
			"startxref",
			"0",
			"%%EOF",
		].join("\n");

		return new Blob([content], { type: "application/pdf" });
	}

	function escapePdfText(s) {
		return String(s ?? "")
			.replaceAll("\\", "\\\\")
			.replaceAll("(", "\\(")
			.replaceAll(")", "\\)");
	}
</script>

<style scoped>
	/* Page base */
	.page {
		min-height: 100vh;
		background: #f3f6ff;
	}

	.content {
		padding: 22px;
	}

	/* Header */
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
		background: rgba(59, 130, 246, 0.1);
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

	/* Cards */
	.card {
		background: rgba(255, 255, 255, 0.92);
		border: 1px solid rgba(15, 23, 42, 0.08);
		border-radius: 16px;
		box-shadow: 0 16px 30px rgba(10, 20, 70, 0.08);
	}

		.card.big {
			padding: 18px;
			margin-bottom: 16px;
		}

	.card-title {
		font-weight: 900;
		color: #1e293b;
		font-size: 16px;
		margin-bottom: 14px;
	}

	/* ✅ Generar Reporte layout */
	.gen-grid {
		display: grid !important;
		grid-template-columns: minmax(0, 1.2fr) minmax(0, 1.8fr);
		gap: 18px 26px !important;
		align-items: end;
		min-width: 0;
	}

	/* ✅ Fechas separadas sí o sí */
	.datesRow {
		display: flex;
		gap: 26px; /* separación real */
		min-width: 0;
	}

		.datesRow .field {
			flex: 1;
			min-width: 0;
		}

	.field label {
		display: block;
		margin-bottom: 8px;
		font-weight: 800;
		color: #64748b;
		font-size: 13px;
	}

	.field input {
		width: 100%;
		box-sizing: border-box;
		border: 1px solid rgba(148, 163, 184, 0.55);
		border-radius: 12px;
		padding: 12px 14px;
		font-size: 14px;
		outline: none;
		background: #fff;
	}

		.field input:focus {
			border-color: rgba(59, 130, 246, 0.65);
			box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.18);
		}

	/* select */
	.selectWrap {
		position: relative;
	}

		.selectWrap select {
			width: 100%;
			appearance: none;
			-webkit-appearance: none;
			-moz-appearance: none;
			box-sizing: border-box;
			border: 1px solid rgba(148, 163, 184, 0.55);
			border-radius: 12px;
			padding: 12px 42px 12px 14px;
			font-size: 14px;
			outline: none;
			background: #fff;
		}

			.selectWrap select:focus {
				border-color: rgba(59, 130, 246, 0.65);
				box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.18);
			}

	.chev {
		position: absolute;
		right: 14px;
		top: 50%;
		transform: translateY(-50%);
		color: #64748b;
		font-weight: 900;
		pointer-events: none;
	}

	/* date wrapper (por si el navegador agrega iconos) */
	.dateWrap {
		width: 100%;
	}

		.dateWrap input {
			width: 100%;
		}

	/* Button generar */
	.btnGen {
		margin-top: 14px;
		border: 0;
		cursor: pointer;
		padding: 12px 18px;
		border-radius: 12px;
		color: #fff;
		font-weight: 900;
		background: linear-gradient(180deg, #2f74ff, #1e5ae9);
		box-shadow: 0 14px 28px rgba(37, 99, 235, 0.25);
		display: inline-flex;
		align-items: center;
		gap: 10px;
	}

		.btnGen:disabled {
			opacity: 0.7;
			cursor: not-allowed;
		}

	.btnIco {
		width: 18px;
		height: 18px;
		display: inline-grid;
		place-items: center;
		font-weight: 900;
	}

	/* grid 3 cards */
	.grid3 {
		display: grid;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		gap: 16px;
		margin-bottom: 16px;
	}

	.card.mini {
		padding: 18px;
		min-height: 150px;
	}

	.mini-ico {
		width: 46px;
		height: 46px;
		border-radius: 14px;
		display: grid;
		place-items: center;
		font-size: 18px;
		margin-bottom: 10px;
	}

		.mini-ico.blue {
			background: rgba(59, 130, 246, 0.12);
		}

		.mini-ico.green {
			background: rgba(34, 197, 94, 0.1);
		}

		.mini-ico.red {
			background: rgba(239, 68, 68, 0.1);
		}

	.mini-title {
		font-weight: 900;
		color: #0f172a;
		margin-bottom: 6px;
	}

	.mini-sub {
		color: #64748b;
		font-weight: 700;
		font-size: 13px;
		margin-bottom: 10px;
	}

	.link {
		border: 0;
		background: transparent;
		color: #2563eb;
		font-weight: 900;
		cursor: pointer;
		padding: 0;
	}

	/* Recent reports */
	.card.recent {
		padding: 18px;
	}

	.recent-list {
		margin-top: 8px;
		display: flex;
		flex-direction: column;
		gap: 10px;
	}

	.recent-row {
		display: flex;
		align-items: center;
		gap: 12px;
		padding: 12px;
		border-radius: 14px;
		border: 1px solid rgba(15, 23, 42, 0.06);
		background: rgba(248, 250, 252, 0.6);
	}

	.docIco {
		width: 36px;
		height: 36px;
		border-radius: 12px;
		background: rgba(59, 130, 246, 0.1);
		display: grid;
		place-items: center;
	}

	.recent-info {
		flex: 1;
		min-width: 0;
	}

	.recent-name {
		font-weight: 900;
		color: #0f172a;
	}

	.recent-date {
		margin-top: 2px;
		font-weight: 700;
		font-size: 12px;
		color: #64748b;
	}

	.dl {
		width: 42px;
		height: 42px;
		border-radius: 14px;
		border: 1px solid rgba(15, 23, 42, 0.08);
		background: rgba(255, 255, 255, 0.95);
		cursor: pointer;
	}

	/* Toast */
	.toast {
		position: fixed;
		right: 18px;
		bottom: 18px;
		padding: 12px 14px;
		border-radius: 14px;
		font-weight: 900;
		box-shadow: 0 16px 30px rgba(10, 20, 70, 0.12);
		border: 1px solid rgba(15, 23, 42, 0.08);
		background: #fff;
		color: #0f172a;
		z-index: 9999;
	}

		.toast.ok {
			border-color: rgba(34, 197, 94, 0.25);
			background: rgba(34, 197, 94, 0.1);
		}

		.toast.warn {
			border-color: rgba(239, 68, 68, 0.25);
			background: rgba(239, 68, 68, 0.08);
		}

	/* Responsive */
	@media (max-width: 1100px) {
		.grid3 {
			grid-template-columns: 1fr;
		}

		.gen-grid {
			grid-template-columns: 1fr;
		}

		.datesRow {
			flex-direction: column;
			gap: 14px;
		}
	}
</style>
