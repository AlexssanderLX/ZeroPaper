export function BrandMark({
  small = false,
  variant = "full",
  priority = false,
}: {
  small?: boolean;
  variant?: "badge" | "full";
  priority?: boolean;
}) {
  // Nova logo quadrada — usada em todos os contextos
  const src = "/brand/zeropaper-logo-512.png?v=20260704-2";

  return (
    <div
      className={`brand-mark${small ? " small" : ""}${variant === "full" ? " full" : ""}`}
      aria-hidden="true"
    >
      <img
        className="brand-mark-image"
        src={src}
        alt=""
        width={512}
        height={512}
        loading={priority ? "eager" : "lazy"}
        decoding="async"
        fetchPriority={priority ? "high" : "auto"}
        style={{ width: "100%", height: "100%", objectFit: "contain" }}
      />
    </div>
  );
}
