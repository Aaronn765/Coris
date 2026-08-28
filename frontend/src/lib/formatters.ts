export function formatDuration(minutes?: number | null, startedAt?: string | null) {
  if (minutes === undefined || minutes === null) {
    return startedAt ? `En cours depuis le ${formatDate(startedAt)}` : "En cours";
  }

  if (minutes < 60) return `${minutes} min`;

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  if (minutes < 24 * 60) {
    return remainingMinutes ? `${hours} h ${remainingMinutes} min` : `${hours} h`;
  }

  const days = Math.floor(minutes / (24 * 60));
  const remainingHours = Math.floor((minutes % (24 * 60)) / 60);
  return remainingHours ? `${days} j ${remainingHours} h` : `${days} j`;
}

export function formatDate(value?: string | null) {
  return value ? new Date(value).toLocaleDateString("fr-FR") : "—";
}

export function formatDateTime(value?: string | null) {
  if (!value) return "—";
  return new Date(value).toLocaleString("fr-FR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}
