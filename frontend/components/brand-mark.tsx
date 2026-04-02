export function BrandMark({
  small = false,
  variant = "full",
}: {
  small?: boolean;
  variant?: "badge" | "full";
}) {
  const src =
    variant === "full"
      ? "/brand/zeropaper-logo.png?v=20260328-1"
      : "/brand/zeropaper-mark.svg?v=20260328-1";

  return (
    <div className={`brand-mark${small ? " small" : ""}${variant === "full" ? " full" : ""}`} aria-hidden="true">
      <img className="brand-mark-image" src={src} alt="" />
    </div>
  );
}
