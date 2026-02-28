<template>
	<div class="page">
		<div class="content">
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
			</div>

			<div class="card big">
				<div class="card-title">Generar Reporte</div>

				<div class="gen-grid">
					<div class="field">
						<label>Tipo de Reporte</label>
						<div class="selectWrap">
							<select v-model="form.tipo" :disabled="generating">
								<option value="stock-actual">Stock Actual</option>
								<option value="movimientos-mes">Movimientos (Mes/Año)</option>
								<option value="stock-critico">Stock Crítico</option>
							</select>
							<span class="chev" aria-hidden="true">▾</span>
						</div>
					</div>

					<div class="datesRow">
						<div class="field">
							<label>Mes</label>
							<div class="selectWrap">
								<select v-model.number="form.mes" :disabled="generating || form.tipo !== 'movimientos-mes'">
									<option v-for="m in meses" :key="m.value" :value="m.value">{{ m.label }}</option>
								</select>
								<span class="chev" aria-hidden="true">▾</span>
							</div>
						</div>

						<div class="field">
							<label>Año</label>
							<div class="dateWrap">
								<input type="number"
									   min="2000"
									   max="2100"
									   v-model.number="form.anio"
									   :disabled="generating || form.tipo !== 'movimientos-mes'" />
							</div>
						</div>
					</div>
				</div>

				<div class="hint" v-if="form.tipo !== 'movimientos-mes'">
					Stock Actual y Stock Crítico no requieren fechas.
				</div>

				<div v-if="loadError" class="hintErr">
					{{ loadError }}
				</div>

				<button class="btnGen" type="button" :disabled="generating" @click="generateAndDownload">
					<span class="btnIco" aria-hidden="true">＋</span>
					{{ generating ? "Generando..." : "Generar y Descargar Reporte" }}
				</button>
			</div>

			<div class="grid3">
				<div class="card mini">
					<div class="mini-ico blue">📄</div>
					<div class="mini-title">Reporte de Stock Actual</div>
					<div class="mini-sub">Consulta el inventario actual de todos los productos</div>
					<button class="link" type="button" :disabled="generating" @click="quickReport('stock-actual')">Ver Reporte →</button>
				</div>

				<div class="card mini">
					<div class="mini-ico green">📅</div>
					<div class="mini-title">Movimientos (Mes/Año)</div>
					<div class="mini-sub">Historial de entradas y salidas por mes y año</div>
					<button class="link" type="button" :disabled="generating" @click="quickReport('movimientos-mes')">Ver Reporte →</button>
				</div>

				<div class="card mini">
					<div class="mini-ico red">⏳</div>
					<div class="mini-title">Stock Crítico</div>
					<div class="mini-sub">Productos que requieren reposición inmediata</div>
					<button class="link" type="button" :disabled="generating" @click="quickReport('stock-critico')">Ver Reporte →</button>
				</div>
			</div>

			<div class="card recent">
				<div class="card-title">Reportes Recientes</div>

				<div v-if="recentReports.length === 0" class="mutedEmpty">
					Aún no has generado reportes.
				</div>

				<div v-else class="recent-list">
					<div class="recent-row" v-for="r in recentReports" :key="r.id">
						<div class="docIco" aria-hidden="true">📄</div>
						<div class="recent-info">
							<div class="recent-name">{{ r.nombre }}</div>
							<div class="recent-date">{{ r.fecha }}</div>
						</div>
						<button class="dl"
								type="button"
								title="Descargar"
								aria-label="Descargar"
								:disabled="generating"
								@click="downloadFromUrl(r.url, r.fileName, r.tipo, r.mes, r.anio)">
							⬇
						</button>
					</div>
				</div>
			</div>

			<div v-if="toast.msg" class="toast" :class="toast.kind">{{ toast.msg }}</div>
		</div>
	</div>
</template>

<script setup>
	import { onMounted, reactive, ref, computed } from "vue";
	import { getUser } from "../router/auth.service";
	import { getPermsSafe } from "../services/permissions";
	import { apiFetch } from "../services/api";

	const user = computed(() => getUser());
	const perms = computed(() => getPermsSafe(user.value));
	const canDownload = computed(
		() =>
			perms.value?.canDownloadReportes === true ||
			perms.value?.canReadAll === true ||
			perms.value?.canEditAll === true
	);

	const PRODUCTOS_ENDPOINT = `/api/Productos`;
	const MOVS_ENDPOINT = `/api/Movimientos`;
	const CATEGORIAS_ENDPOINT = `/api/Categorias`;

	const USUARIOS_ENDPOINTS = [`/api/Usuarios`];
	const MOTIVOS_ENDPOINTS = [`/api/MotivosMovimiento`];

	const generating = ref(false);
	const loadError = ref("");

	const meses = [
		{ value: 1, label: "Enero" },
		{ value: 2, label: "Febrero" },
		{ value: 3, label: "Marzo" },
		{ value: 4, label: "Abril" },
		{ value: 5, label: "Mayo" },
		{ value: 6, label: "Junio" },
		{ value: 7, label: "Julio" },
		{ value: 8, label: "Agosto" },
		{ value: 9, label: "Septiembre" },
		{ value: 10, label: "Octubre" },
		{ value: 11, label: "Noviembre" },
		{ value: 12, label: "Diciembre" },
	];

	function nowMonthYear() {
		const d = new Date();
		return { mes: d.getMonth() + 1, anio: d.getFullYear() };
	}

	const form = reactive({
		tipo: "stock-actual",
		mes: nowMonthYear().mes,
		anio: nowMonthYear().anio,
	});

	const toast = reactive({ msg: "", kind: "ok" });
	let toastTimer = null;

	function showToast(msg, kind = "ok") {
		toast.msg = msg;
		toast.kind = kind;
		clearTimeout(toastTimer);
		toastTimer = setTimeout(() => (toast.msg = ""), 2600);
	}

	const recentReports = ref([]);

	onMounted(() => {
		try {
			const saved = localStorage.getItem("sm_recent_reports");
			if (saved) {
				const parsed = JSON.parse(saved);
				if (Array.isArray(parsed)) recentReports.value = parsed.slice(0, 10);
			}
		} catch { }
	});

	function persistRecent() {
		try {
			localStorage.setItem(
				"sm_recent_reports",
				JSON.stringify(recentReports.value.slice(0, 10))
			);
		} catch { }
	}

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

	function toNumber(v, d = 0) {
		const n = Number(v);
		return Number.isFinite(n) ? n : d;
	}

	async function apiFetchJson(url, opts = {}) {
		try {
			return await apiFetch(url, opts);
		} catch (e) {
			throw new Error(`${e?.message || "Error"} [${url}]`);
		}
	}

	async function fetchFirstList(endpoints) {
		let lastErr = null;
		for (const url of endpoints) {
			try {
				const data = await apiFetchJson(url);
				return { url, list: normalizeList(data) };
			} catch (e) {
				lastErr = e;
			}
		}
		return {
			url: endpoints[0],
			list: [],
			error: lastErr?.message || "No se pudo cargar lista.",
		};
	}

	function extractCategoriaNombre(raw) {
		const c =
			raw?.categoria ?? raw?.Categoria ?? raw?.category ?? raw?.categoriaDto ?? null;

		if (c && typeof c === "object") {
			const name = c?.nombre ?? c?.name ?? c?.descripcion ?? c?.description ?? "";
			return String(name || "—").trim();
		}

		const asStr = raw?.categoriaNombre || raw?.categoryName || c;
		if (asStr && String(asStr).trim()) return String(asStr).trim();

		const id = raw?.idCategoria ?? raw?.categoriaId ?? raw?.IdCategoria ?? null;
		return id ? `ID ${id}` : "—";
	}

	function normalizeProducto(p, idx, categoriaById) {
		const id =
			p?.idProducto ?? p?.IdProducto ?? p?.id ?? p?.Id ?? p?.productoId ?? idx;

		const nombre = p?.nombre ?? p?.Nombre ?? p?.descripcion ?? p?.Descripcion ?? p?.name ?? "";
		const stock = toNumber(
			p?.stockActual ?? p?.StockActual ?? p?.stock ?? p?.existencia ?? p?.cantidad ?? p?.qty,
			0
		);
		const minimo = toNumber(
			p?.stockMinimo ?? p?.StockMinimo ?? p?.minimo ?? p?.minStock ?? p?.reorderLevel,
			0
		);

		const idCat = p?.idCategoria ?? p?.IdCategoria ?? p?.categoriaId ?? p?.CategoriaId ?? null;

		const categoriaNombre =
			p?.categoria?.nombre ??
			p?.Categoria?.Nombre ??
			categoriaById?.get(Number(idCat)) ??
			extractCategoriaNombre(p);

		return {
			id: Number(id),
			nombre: String(nombre ?? "").trim(),
			categoriaNombre: String(categoriaNombre ?? "—").trim() || "—",
			stock,
			minimo,
			_raw: p,
		};
	}

	function normalizeUsuario(u, idx) {
		const id = u?.idUsuario ?? u?.IdUsuario ?? u?.id ?? u?.Id ?? idx;
		const nombreCompleto =
			u?.nombreCompleto ??
			u?.NombreCompleto ??
			u?.nombre ??
			u?.Nombre ??
			u?.username ??
			u?.Username ??
			u?.email ??
			u?.Email ??
			"";

		return { id: Number(id), nombre: String(nombreCompleto ?? "").trim(), _raw: u };
	}

	function normalizeMotivo(mm, idx) {
		const id =
			mm?.idMotivo ??
			mm?.IdMotivo ??
			mm?.id ??
			mm?.Id ??
			mm?.idMotivoMovimiento ??
			mm?.IdMotivoMovimiento ??
			idx;

		const nombre = mm?.nombre ?? mm?.Nombre ?? mm?.descripcion ?? mm?.Descripcion ?? "";
		return { id: Number(id), nombre: String(nombre ?? "").trim(), _raw: mm };
	}

	function normalizeMovimiento(m, idx) {
		const id = m?.idMovimiento ?? m?.IdMovimiento ?? m?.id ?? m?.movimientoId ?? idx;

		const rawDate =
			m?.fecha ?? m?.Fecha ?? m?.createdAt ?? m?.CreatedAt ?? m?.fechaMovimiento ?? m?.date ?? null;
		const dt = rawDate ? new Date(rawDate) : null;

		let tipo = m?.tipo ?? m?.Tipo ?? m?.tipoMovimiento ?? m?.movementType ?? "Entrada";
		if (typeof tipo === "number") tipo = tipo === 1 ? "Entrada" : "Salida";
		if (typeof tipo === "string") {
			const t = tipo.toLowerCase();
			if (t.startsWith("e")) tipo = "Entrada";
			else if (t.startsWith("s")) tipo = "Salida";
			else if (t === "entrada" || t === "salida") tipo = t[0].toUpperCase() + t.slice(1);
		}

		const prodRaw = m?.producto ?? m?.Producto ?? m?.product ?? m?.Product ?? null;
		const prodObj = (prodRaw && typeof prodRaw === "object") ? prodRaw : null;

		const idProducto =
			Number(
				m?.idProducto ??
				m?.IdProducto ??
				m?.productoId ??
				m?.ProductoId ??
				m?.idproducto ??
				m?.producto_id ??
				0
			) ||
			Number(
				prodObj?.idProducto ??
				prodObj?.IdProducto ??
				prodObj?.id ??
				prodObj?.Id ??
				prodObj?.productoId ??
				prodObj?.ProductoId ??
				0
			) ||
			null;

		const idUsuario = Number(m?.idUsuario ?? m?.IdUsuario ?? m?.usuarioId ?? 0) || null;

		const idMotivo =
			Number(
				m?.idMotivo ??
				m?.IdMotivo ??
				m?.idMotivoMovimiento ??
				m?.IdMotivoMovimiento ??
				m?.motivoId ??
				0
			) || null;

		const motivoDirecto =
			m?.motivoNombre ??
			m?.MotivoNombre ??
			m?.motivo?.nombre ??
			m?.motivo?.descripcion ??
			m?.Motivo?.Nombre ??
			m?.motivo ??
			m?.comentario ??
			m?.reason ??
			"";

		const prodDirecto =
			(typeof prodRaw === "string" ? prodRaw : "") ||
			prodObj?.nombre ||
			prodObj?.Nombre ||
			prodObj?.descripcion ||
			prodObj?.Descripcion ||
			m?.productoNombre ||
			m?.ProductoNombre ||
			m?.nombreProducto ||
			m?.NombreProducto ||
			"";

		return {
			id,
			_dt: dt && !isNaN(dt.getTime()) ? dt : null,
			fechaIso: dt && !isNaN(dt.getTime()) ? dt.toISOString().slice(0, 10) : "",
			tipo,
			idProducto,
			idUsuario,
			idMotivo,
			motivo: String(motivoDirecto ?? "").trim() || "",
			productoNombre: String(prodDirecto ?? "").trim() || "",
			cantidad: toNumber(m?.cantidad ?? m?.Cantidad ?? m?.qty ?? 0, 0),
			_raw: m,
		};
	}

	function buildFileName(tipo, mes, anio) {
		if (tipo === "movimientos-mes") return `Reporte_Movimientos_${anio}_${String(mes).padStart(2, "0")}.pdf`;
		if (tipo === "stock-actual") return `Reporte_Stock_Actual_${new Date().toISOString().slice(0, 10).replaceAll("-", "")}.pdf`;
		return `Reporte_Stock_Critico_${new Date().toISOString().slice(0, 10).replaceAll("-", "")}.pdf`;
	}

	function monthStartFrom(mes, anio) {
		return new Date(anio, mes - 1, 1);
	}

	function monthEndFrom(mes, anio) {
		return new Date(anio, mes, 0);
	}

	function ymdToday() {
		return new Date().toISOString().slice(0, 10);
	}

	function clip(s, n) {
		const t = String(s ?? "");
		return t.length <= n ? t : t.slice(0, Math.max(0, n - 1)) + "…";
	}

	function toAsciiSafe(s) {
		let t = String(s ?? "");
		t = t.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
		t = t.replace(/[“”]/g, '"').replace(/[‘’´`]/g, "'").replaceAll("—", "-");
		t = t.replace(/[^\x20-\x7E]/g, "");
		return t;
	}

	function escapePdfText(s) {
		const t = toAsciiSafe(s);
		return t.replaceAll("\\", "\\\\").replaceAll("(", "\\(").replaceAll(")", "\\)");
	}

	function pdfBuildFromPages(pages) {
		const objects = [];
		const offsets = [];

		function addObject(body) {
			objects.push(body);
			return objects.length;
		}

		const fontHelv = addObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
		const fontCour = addObject("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");

		const pageObjNums = [];

		for (const p of pages) {
			const contentStream = p.content;
			const len = new TextEncoder().encode(contentStream).length;

			const contentsObj = addObject(
				`<< /Length ${len} >>\nstream\n${contentStream}\nendstream`
			);

			const pageObj = addObject(
				`<< /Type /Page /Parent PAGES_REF /MediaBox [0 0 612 792] /Resources << /Font << /F1 ${fontHelv} 0 R /F2 ${fontCour} 0 R >> >> /Contents ${contentsObj} 0 R >>`
			);

			pageObjNums.push(pageObj);
		}

		const kids = pageObjNums.map((n) => `${n} 0 R`).join(" ");
		const pagesObj = addObject(`<< /Type /Pages /Kids [${kids}] /Count ${pageObjNums.length} >>`);
		const catalogObj = addObject(`<< /Type /Catalog /Pages ${pagesObj} 0 R >>`);

		for (let i = 0; i < objects.length; i++) {
			objects[i] = objects[i].replaceAll("PAGES_REF", `${pagesObj} 0 R`);
		}

		let pdf = "%PDF-1.4\n";
		offsets.push(0);

		for (let i = 0; i < objects.length; i++) {
			offsets.push(new TextEncoder().encode(pdf).length);
			pdf += `${i + 1} 0 obj\n${objects[i]}\nendobj\n`;
		}

		const xrefStart = new TextEncoder().encode(pdf).length;
		pdf += "xref\n";
		pdf += `0 ${objects.length + 1}\n`;
		pdf += "0000000000 65535 f \n";

		for (let i = 1; i <= objects.length; i++) {
			const off = offsets[i];
			pdf += `${String(off).padStart(10, "0")} 00000 n \n`;
		}

		pdf += `trailer\n<< /Size ${objects.length + 1} /Root ${catalogObj} 0 R >>\n`;
		pdf += "startxref\n";
		pdf += `${xrefStart}\n`;
		pdf += "%%EOF";

		return new Blob([pdf], { type: "application/pdf" });
	}

	function pdfPagesFromLines({ title, subtitle, lines }) {
		const maxLinesPerPage = 54;
		const pages = [];
		const chunks = [];

		for (let i = 0; i < lines.length; i += maxLinesPerPage) {
			chunks.push(lines.slice(i, i + maxLinesPerPage));
		}
		if (chunks.length === 0) chunks.push([]);

		for (let pi = 0; pi < chunks.length; pi++) {
			const bodyLines = chunks[pi];
			const yTitle = 740;
			const ySub = 712;
			const yBodyStart = 680;
			const lineH = 12;

			const parts = [];
			parts.push("BT");
			parts.push("/F1 18 Tf");
			parts.push(`72 ${yTitle} Td`);
			parts.push(`(${escapePdfText(title)}) Tj`);
			parts.push("ET");

			parts.push("BT");
			parts.push("/F1 12 Tf");
			parts.push(`72 ${ySub} Td`);
			parts.push(`(${escapePdfText(subtitle)}) Tj`);
			parts.push("ET");

			parts.push("BT");
			parts.push("/F2 10 Tf");
			parts.push(`72 ${yBodyStart} Td`);

			for (let i = 0; i < bodyLines.length; i++) {
				const txt = escapePdfText(bodyLines[i]);
				parts.push(`(${txt}) Tj`);
				if (i !== bodyLines.length - 1) parts.push(`0 -${lineH} Td`);
			}

			parts.push("ET");

			const footer = `Pagina ${pi + 1} de ${chunks.length}`;
			parts.push("BT");
			parts.push("/F1 10 Tf");
			parts.push("72 40 Td");
			parts.push(`(${escapePdfText(footer)}) Tj`);
			parts.push("ET");

			pages.push({ content: parts.join("\n") });
		}

		return pages;
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

	function col(val, width, align = "left") {
		const t = toAsciiSafe(val);
		if (t.length === width) return t;
		if (t.length > width) return clip(t, width);
		if (align === "right") return t.padStart(width, " ");
		return t.padEnd(width, " ");
	}

	function rowSep(totalWidth) {
		return "-".repeat(totalWidth);
	}

	async function buildReportPdf(tipo, mes, anio) {
		if (!canDownload.value) {
			throw new Error("Tu rol está en solo lectura y no tiene permiso para descargar reportes.");
		}

		loadError.value = "";

		const [prodsRaw, catsRaw, usersPack, motivosPack, movsRaw] = await Promise.all([
			apiFetchJson(PRODUCTOS_ENDPOINT),
			apiFetchJson(CATEGORIAS_ENDPOINT),
			fetchFirstList(USUARIOS_ENDPOINTS),
			fetchFirstList(MOTIVOS_ENDPOINTS),
			apiFetchJson(MOVS_ENDPOINT),
		]);

		if (usersPack?.error) loadError.value = usersPack.error;
		if (motivosPack?.error)
			loadError.value = loadError.value
				? `${loadError.value} | ${motivosPack.error}`
				: motivosPack.error;

		const categorias = normalizeList(catsRaw);
		const categoriaById = new Map(
			categorias.map((c) => [
				Number(c.idCategoria ?? c.IdCategoria ?? c.id ?? c.Id),
				String(c.nombre ?? c.Nombre ?? "—").trim() || "—",
			])
		);

		const productos = normalizeList(prodsRaw)
			.map((p, i) => normalizeProducto(p, i, categoriaById))
			.filter((p) => p.id && p.nombre);

		const usuarios = normalizeList(usersPack.list)
			.map((u, i) => normalizeUsuario(u, i))
			.filter((u) => u.id && u.nombre);

		const motivos = normalizeList(motivosPack.list)
			.map((m, i) => normalizeMotivo(m, i))
			.filter((m) => m.id && m.nombre);

		const movimientos = normalizeList(movsRaw).map((m, i) => normalizeMovimiento(m, i));

		const prodById = new Map(productos.map((p) => [Number(p.id), p]));
		const userById = new Map(usuarios.map((u) => [Number(u.id), u]));
		const motivoById = new Map(motivos.map((mm) => [Number(mm.id), mm]));

		if (tipo === "stock-actual") {
			const t = "Reporte: Stock Actual";
			const sub = `Fecha: ${ymdToday()}   Total: ${productos.length}`;

			const totalW = 96;
			const lines = [];
			lines.push(`| ${col("ID", 5)} | ${col("PRODUCTO", 34)} | ${col("CATEGORIA", 26)} | ${col("STOCK", 9, "right")} | ${col("MIN", 7, "right")} |`);
			lines.push(rowSep(totalW));

			const ordered = [...productos].sort((a, b) =>
				toAsciiSafe(a.nombre).localeCompare(toAsciiSafe(b.nombre))
			);

			for (const p of ordered) {
				lines.push(`| ${col(p.id, 5)} | ${col(p.nombre, 34)} | ${col(p.categoriaNombre || "—", 26)} | ${col(p.stock, 9, "right")} | ${col(p.minimo, 7, "right")} |`);
			}

			const pages = pdfPagesFromLines({ title: t, subtitle: sub, lines });
			return pdfBuildFromPages(pages);
		}

		if (tipo === "stock-critico") {
			const crit = productos.filter(
				(p) => toNumber(p.minimo, 0) > 0 && toNumber(p.stock, 0) <= toNumber(p.minimo, 0)
			);

			const t = "Reporte: Stock Critico";
			const sub = `Fecha: ${ymdToday()}   Total criticos: ${crit.length}`;

			const totalW = 96;
			const lines = [];
			lines.push(`| ${col("ID", 5)} | ${col("PRODUCTO", 34)} | ${col("CATEGORIA", 26)} | ${col("STOCK", 9, "right")} | ${col("MIN", 7, "right")} |`);
			lines.push(rowSep(totalW));

			const ordered = [...crit].sort(
				(a, b) => (toNumber(a.stock) - toNumber(a.minimo)) - (toNumber(b.stock) - toNumber(b.minimo))
			);

			for (const p of ordered) {
				lines.push(`| ${col(p.id, 5)} | ${col(p.nombre, 34)} | ${col(p.categoriaNombre || "—", 26)} | ${col(p.stock, 9, "right")} | ${col(p.minimo, 7, "right")} |`);
			}

			const pages = pdfPagesFromLines({ title: t, subtitle: sub, lines });
			return pdfBuildFromPages(pages);
		}

		const from = monthStartFrom(mes, anio);
		const to = monthEndFrom(mes, anio);
		const fromIso = from.toISOString().slice(0, 10);
		const toIso = to.toISOString().slice(0, 10);
		const toEnd = new Date(to.getFullYear(), to.getMonth(), to.getDate(), 23, 59, 59, 999);

		const list = movimientos
			.filter((m) => m._dt && m._dt >= from && m._dt <= toEnd)
			.sort((a, b) => (b._dt ? b._dt.getTime() : 0) - (a._dt ? a._dt.getTime() : 0));

		const t = "Reporte: Movimientos del Mes";
		const mesLabel = meses.find((x) => x.value === Number(mes))?.label || String(mes);
		const sub = `Periodo: ${mesLabel} ${anio} (${fromIso} a ${toIso})   Total: ${list.length}`;

		const totalW = 112;
		const lines = [];
		lines.push(`| ${col("FECHA", 10)} | ${col("TIPO", 8)} | ${col("CANT", 6, "right")} | ${col("PRODUCTO", 34)} | ${col("MOTIVO", 20)} | ${col("USUARIO", 22)} |`);
		lines.push(rowSep(totalW));

		for (const m of list) {
			const prod =
				(m.productoNombre && m.productoNombre.trim())
					? m.productoNombre
					: (prodById.get(Number(m.idProducto))?.nombre || "—");

			const usu = userById.get(Number(m.idUsuario))?.nombre || "—";
			const mot =
				(m.motivo?.trim() || "") ||
				motivoById.get(Number(m.idMotivo))?.nombre ||
				"—";

			lines.push(`| ${col(m.fechaIso || "", 10)} | ${col(m.tipo, 8)} | ${col(m.cantidad, 6, "right")} | ${col(prod, 34)} | ${col(mot, 20)} | ${col(usu, 22)} |`);
		}

		const pages = pdfPagesFromLines({ title: t, subtitle: sub, lines });
		return pdfBuildFromPages(pages);
	}

	async function generateAndDownload() {
		if (!canDownload.value) {
			showToast("Solo lectura: sin permiso para descargar reportes.", "warn");
			return;
		}

		generating.value = true;
		try {
			const tipo = form.tipo;
			const mes = form.mes;
			const anio = form.anio;

			const blob = await buildReportPdf(tipo, mes, anio);
			const fileName = buildFileName(tipo, mes, anio);
			downloadBlob(blob, fileName);

			const nombre =
				tipo === "stock-actual"
					? "Reporte de Stock Actual"
					: tipo === "movimientos-mes"
						? `Movimientos (${String(mes).padStart(2, "0")}/${anio})`
						: "Stock Crítico";

			recentReports.value.unshift({
				id: Date.now(),
				tipo,
				nombre,
				fecha: new Date().toLocaleString("es-DO"),
				url: "",
				fileName,
				mes,
				anio,
			});

			recentReports.value = recentReports.value.slice(0, 10);
			persistRecent();

			showToast("Reporte generado y descargado ✅", "ok");
		} catch (e) {
			showToast(e?.message || "No se pudo generar el reporte.", "warn");
		} finally {
			generating.value = false;
		}
	}

	function quickReport(tipo) {
		form.tipo = tipo;
		generateAndDownload();
	}

	function downloadFromUrl(url, fileName, tipo, mes, anio) {
		if (!canDownload.value) {
			showToast("Solo lectura: sin permiso para descargar reportes.", "warn");
			return;
		}

		if (url) {
			const a = document.createElement("a");
			a.href = url;
			a.download = fileName || "reporte.pdf";
			document.body.appendChild(a);
			a.click();
			a.remove();
			return;
		}

		form.tipo = tipo || form.tipo;
		if (tipo === "movimientos-mes") {
			form.mes = mes || form.mes;
			form.anio = anio || form.anio;
		}
		generateAndDownload();
	}
</script>

<style scoped>
	.page {
		min-height: 100vh;
		background: #f3f6ff;
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

	.gen-grid {
		display: grid !important;
		grid-template-columns: minmax(0, 1.2fr) minmax(0, 1.8fr);
		gap: 18px 26px !important;
		align-items: end;
		min-width: 0;
	}

	.datesRow {
		display: flex;
		gap: 26px;
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

	.dateWrap {
		width: 100%;
	}

		.dateWrap input {
			width: 100%;
		}

	.hint {
		margin-top: 10px;
		color: #64748b;
		font-weight: 800;
		font-size: 12px;
	}

	.hintErr {
		margin-top: 10px;
		color: #b45309;
		font-weight: 900;
		font-size: 12px;
	}

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

		.link:disabled {
			opacity: 0.6;
			cursor: not-allowed;
		}

	.card.recent {
		padding: 18px;
	}

	.mutedEmpty {
		margin-top: 8px;
		color: #64748b;
		font-weight: 800;
		font-size: 13px;
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

		.dl:disabled {
			opacity: 0.6;
			cursor: not-allowed;
		}

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