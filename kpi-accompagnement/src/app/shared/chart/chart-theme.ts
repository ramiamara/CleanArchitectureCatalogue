import { Chart } from 'chart.js';

/** Palette partagée entre SCSS et Chart.js (garder synchro avec styles.scss). */
export const COLORS = {
  navy: '#17375e',
  individuel: '#e8762c',
  collectif: '#1b9fd8',
  positif: '#1f8f4e',
  negatif: '#d4521b',
  neutre: '#9aa5b4',
  grid: '#e6eaf0',
  text: '#4a5566',
  series: ['#17375e', '#1b9fd8', '#e8762c', '#1f8f4e', '#8b5cf6', '#c2410c', '#0f766e', '#b45309', '#64748b', '#be185d'],
};

export function appliquerThemeChart() {
  Chart.defaults.font.family = "'Source Sans 3', system-ui, sans-serif";
  Chart.defaults.font.size = 12;
  Chart.defaults.color = COLORS.text;
  Chart.defaults.borderColor = COLORS.grid;
  Chart.defaults.plugins.legend.labels.boxWidth = 10;
  Chart.defaults.plugins.legend.labels.boxHeight = 10;
  Chart.defaults.plugins.legend.labels.useBorderRadius = true;
  Chart.defaults.plugins.legend.labels.borderRadius = 2;
  Chart.defaults.plugins.tooltip.backgroundColor = '#0f2340';
  Chart.defaults.plugins.tooltip.padding = 10;
  Chart.defaults.plugins.tooltip.cornerRadius = 6;
  Chart.defaults.elements.bar.borderRadius = 3;
  Chart.defaults.animation = { duration: 350 };
}
