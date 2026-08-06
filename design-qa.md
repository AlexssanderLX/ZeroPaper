**Source visual truth**

- Landing palette reference: `.codex-screenshots/landing-palette-reference.png`
- User-provided problem references: the three attached cadastro screenshots showing the brown treatment to remove.

**Implementation evidence**

- Login: `.codex-screenshots/auth-login-green-yellow.png`
- Cadastro: `.codex-screenshots/auth-signup-green-yellow.png`
- Browser viewport: 1280 x 720 CSS px; captured images normalized by the in-app browser to 823 x 712 px.
- State: unauthenticated login and restaurant cadastro with the Operacao plan selected.
- Primary interactions checked: route navigation, plan selection links, form fields, password visibility controls, payment/pre-registration buttons, and removal of the public simultaneous-user copy.

**Full-view comparison evidence**

- The landing reference uses a blue-black/green background, restrained green glow, yellow highlights and neutral dark cards.
- Login and cadastro now use the same background family, green borders/glows and yellow semantic accents. The brown page background and brown offer cards visible in the problem references are no longer present.

**Focused-region comparison evidence**

- Plan selector and selected-plan card: dark green-neutral surfaces, green active border, yellow recommended badge and price.
- Login illustration and form: green-neutral panels with yellow status/primary-action accents; the former brown panels were replaced.
- Public plan copy: no simultaneous-user limit is rendered, including when the frontend receives stale plan data from an older API deployment.

**Findings**

- No actionable P0, P1 or P2 visual mismatch remains for the requested palette and copy changes.
- P3: the display serif remains intentionally different from the landing hero sans-serif because it is an established authentication-page typography choice and preserves the current hierarchy.

**Comparison history**

- Initial pass found P1 brown backgrounds/cards on login and cadastro and P1 stale simultaneous-user copy returned by the deployed API.
- Fixed by replacing auth/signup palette overrides and illustration surfaces with green/yellow tokens, removing the backend public feature copy, removing the landing audience label, and filtering stale public feature data in the frontend.
- Post-fix browser capture confirms the updated palette and the absence of the limit copy.

**Implementation checklist**

- [x] Match landing green/yellow palette.
- [x] Remove brown auth/signup background and card treatment.
- [x] Remove public simultaneous-user claims.
- [x] Preserve form interactions and responsive CSS rules.
- [x] Verify production frontend build and backend tests.

final result: passed
