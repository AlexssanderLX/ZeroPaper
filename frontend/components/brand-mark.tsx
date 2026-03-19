export function BrandMark({ small = false }: { small?: boolean }) {
  return (
    <div className={`brand-mark${small ? " small" : ""}`} aria-hidden="true">
      <svg className="brand-mark-vine" viewBox="0 0 132 96" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path
          className="brand-mark-stem"
          d="M21 70C33 77 47 80 63 80C85 80 104 72 116 56"
        />
        <path
          className="brand-mark-branch"
          d="M36 69C39 57 45 48 56 40"
        />
        <path
          className="brand-mark-branch"
          d="M80 77C83 63 93 49 108 40"
        />
        <path
          className="brand-mark-leaf"
          d="M19 39C30 32 42 32 50 39C41 47 29 47 19 39Z"
        />
        <path
          className="brand-mark-leaf"
          d="M82 20C93 12 106 12 116 21C106 30 93 31 82 20Z"
        />
        <path
          className="brand-mark-leaf small-leaf"
          d="M54 23C62 17 72 17 80 23C72 30 62 30 54 23Z"
        />
      </svg>

      <div className="brand-mark-letters">
        <span>Z</span>
        <span>P</span>
      </div>
    </div>
  );
}
