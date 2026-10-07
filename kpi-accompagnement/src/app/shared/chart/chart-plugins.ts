import { Chart, Plugin } from 'chart.js';

/** Écrit la valeur (et un suffixe) au centre de chaque barre — utile pour la barre de répartition. */
export const insideBarLabels = (formatter: (value: number, datasetIndex: number) => string): Plugin<'bar'> => ({
  id: 'insideBarLabels',
  afterDatasetsDraw(chart: Chart<'bar'>) {
    const { ctx } = chart;
    ctx.save();
    ctx.font = "700 13px 'Source Sans 3', sans-serif";
    ctx.fillStyle = '#fff';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    chart.data.datasets.forEach((ds, di) => {
      const meta = chart.getDatasetMeta(di);
      if (meta.hidden) return;
      meta.data.forEach((bar, i) => {
        const v = Number(ds.data[i]);
        if (!v) return;
        const { x, y, base } = bar.getProps(['x', 'y', 'base'], true) as { x: number; y: number; base: number };
        const width = Math.abs(x - base);
        const label = formatter(v, di);
        if (ctx.measureText(label).width + 12 > width) return;
        ctx.fillText(label, (x + base) / 2, y);
      });
    });
    ctx.restore();
  },
});

/** Zone grisée + bornes verticales représentant la période filtrée sur un axe X temporel (timestamps). */
export const periodeBand = (debut: number, fin: number, label: string): Plugin<'bar'> => ({
  id: 'periodeBand',
  beforeDatasetsDraw(chart) {
    const x = chart.scales['x'];
    const { top, bottom } = chart.chartArea;
    const x1 = x.getPixelForValue(debut);
    const x2 = x.getPixelForValue(fin);
    const { ctx } = chart;
    ctx.save();
    ctx.fillStyle = 'rgba(27,159,216,0.07)';
    ctx.fillRect(x1, top, x2 - x1, bottom - top);
    ctx.strokeStyle = '#e8762c';
    ctx.lineWidth = 1.5;
    ctx.setLineDash([4, 3]);
    for (const px of [x1, x2]) {
      ctx.beginPath();
      ctx.moveTo(px, top);
      ctx.lineTo(px, bottom);
      ctx.stroke();
    }
    ctx.setLineDash([]);
    ctx.fillStyle = '#e8762c';
    ctx.font = "700 11px 'Source Sans 3', sans-serif";
    ctx.textAlign = 'left';
    ctx.fillText(label, x1 + 4, top + 10);
    ctx.restore();
  },
});

/** Ligne horizontale de référence (ex. moyenne agence, 100 % d'objectif). */
export const referenceLine = (value: number, label: string, color = '#e8762c', axis = 'y'): Plugin => ({
  id: 'referenceLine',
  afterDatasetsDraw(chart) {
    const scale = chart.scales[axis];
    if (!scale) return;
    const { left, right, top, bottom } = chart.chartArea;
    const { ctx } = chart;
    const p = scale.getPixelForValue(value);
    ctx.save();
    ctx.strokeStyle = color;
    ctx.lineWidth = 1.5;
    ctx.setLineDash([5, 4]);
    ctx.beginPath();
    if (axis === 'y') {
      ctx.moveTo(left, p);
      ctx.lineTo(right, p);
    } else {
      ctx.moveTo(p, top);
      ctx.lineTo(p, bottom);
    }
    ctx.stroke();
    ctx.setLineDash([]);
    ctx.fillStyle = color;
    ctx.font = "700 11px 'Source Sans 3', sans-serif";
    ctx.textAlign = 'right';
    if (axis === 'y') ctx.fillText(label, right - 4, p - 5);
    else ctx.fillText(label, p - 4, top + 10);
    ctx.restore();
  },
});
