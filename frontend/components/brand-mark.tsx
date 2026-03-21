export function BrandMark({
  small = false,
  variant = "full",
}: {
  small?: boolean;
  variant?: "badge" | "full";
}) {
  return (
    <div className={`brand-mark${small ? " small" : ""}${variant === "full" ? " full" : ""}`} aria-hidden="true">
      <img
        className="brand-mark-image"
        src={variant === "full" ? "/brand/zeropaper-logo.png" : "/brand/zeropaper-mark.svg"}
        alt=""
      />
    </div>
  );
}
