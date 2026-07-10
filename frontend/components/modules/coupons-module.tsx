"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  createCoupon,
  getCoupons,
  updateCoupon,
  updateCouponStatus,
  type Coupon,
  type SaveCouponPayload,
} from "@/lib/api";
import { formatCurrency, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

type CouponDraft = {
  id: string | null;
  code: string;
  description: string;
  discountType: string;
  discountValue: string;
  minimumOrderAmount: string;
  startsAt: string;
  endsAt: string;
  usageLimit: string;
};

const EMPTY_DRAFT: CouponDraft = {
  id: null,
  code: "",
  description: "",
  discountType: "Percent",
  discountValue: "",
  minimumOrderAmount: "",
  startsAt: "",
  endsAt: "",
  usageLimit: "",
};

function isPercent(discountType: string) {
  return discountType.toLowerCase() === "percent";
}

function formatDateOnly(value?: string | null) {
  if (!value) {
    return null;
  }

  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeZone: "America/Sao_Paulo" }).format(new Date(value));
}

function toDateInput(value?: string | null) {
  return value ? value.slice(0, 10) : "";
}

function formatDiscount(coupon: Coupon) {
  return isPercent(coupon.discountType) ? `${coupon.discountValue}%` : formatCurrency(coupon.discountValue);
}

function formatValidity(coupon: Coupon) {
  const start = formatDateOnly(coupon.startsAtUtc);
  const end = formatDateOnly(coupon.endsAtUtc);

  if (!start && !end) {
    return "Sem prazo";
  }
  if (start && end) {
    return `${start} a ${end}`;
  }
  if (end) {
    return `Ate ${end}`;
  }
  return `A partir de ${start}`;
}

function formatUsage(coupon: Coupon) {
  return coupon.usageLimit != null ? `${coupon.usageCount}/${coupon.usageLimit} usos` : `${coupon.usageCount} usos`;
}

function getCouponStatus(coupon: Coupon) {
  if (!coupon.isActive) {
    return { label: "Pausado", tone: "muted" };
  }

  const now = Date.now();
  if (coupon.startsAtUtc && new Date(coupon.startsAtUtc).getTime() > now) {
    return { label: "Agendado", tone: "info" };
  }
  if (coupon.endsAtUtc && new Date(coupon.endsAtUtc).getTime() < now) {
    return { label: "Expirado", tone: "danger" };
  }
  if (coupon.usageLimit != null && coupon.usageCount >= coupon.usageLimit) {
    return { label: "Esgotado", tone: "danger" };
  }
  return { label: "Ativo", tone: "good" };
}

export function CouponsModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [coupons, setCoupons] = useState<Coupon[]>([]);
  const [loading, setLoading] = useState(true);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [draft, setDraft] = useState<CouponDraft>(EMPTY_DRAFT);
  const [isSaving, setIsSaving] = useState(false);
  const [statusBusyId, setStatusBusyId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadCoupons(silent = false) {
    if (!silent) {
      setLoading(true);
    }

    try {
      const response = await getCoupons(token);
      setCoupons(response);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os cupons.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadCoupons();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  const sortedCoupons = useMemo(
    () => [...coupons].sort((left, right) => left.code.localeCompare(right.code)),
    [coupons],
  );

  function openCreate() {
    setDraft(EMPTY_DRAFT);
    setIsFormOpen(true);
    setSuccessMessage("");
  }

  function openEdit(coupon: Coupon) {
    setDraft({
      id: coupon.id,
      code: coupon.code,
      description: coupon.description ?? "",
      discountType: isPercent(coupon.discountType) ? "Percent" : "FixedAmount",
      discountValue: String(coupon.discountValue ?? ""),
      minimumOrderAmount: coupon.minimumOrderAmount ? String(coupon.minimumOrderAmount) : "",
      startsAt: toDateInput(coupon.startsAtUtc),
      endsAt: toDateInput(coupon.endsAtUtc),
      usageLimit: coupon.usageLimit != null ? String(coupon.usageLimit) : "",
    });
    setIsFormOpen(true);
    setSuccessMessage("");
  }

  function closeForm() {
    setIsFormOpen(false);
    setDraft(EMPTY_DRAFT);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!draft.code.trim()) {
      setErrorMessage("Informe o codigo do cupom.");
      return;
    }

    const payload: SaveCouponPayload = {
      code: draft.code.trim().toUpperCase(),
      description: draft.description.trim() || null,
      discountType: draft.discountType,
      discountValue: Number(draft.discountValue.replace(",", ".")) || 0,
      minimumOrderAmount: Number(draft.minimumOrderAmount.replace(",", ".")) || 0,
      startsAtUtc: draft.startsAt ? `${draft.startsAt}T00:00:00.000Z` : null,
      endsAtUtc: draft.endsAt ? `${draft.endsAt}T23:59:59.999Z` : null,
      usageLimit: draft.usageLimit.trim() ? Number(draft.usageLimit) : null,
    };

    try {
      setIsSaving(true);
      if (draft.id) {
        await updateCoupon(token, draft.id, payload);
        setSuccessMessage("Cupom atualizado.");
      } else {
        await createCoupon(token, payload);
        setSuccessMessage("Cupom criado.");
      }
      setErrorMessage("");
      closeForm();
      await loadCoupons(true);
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar o cupom.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleToggleStatus(coupon: Coupon) {
    try {
      setStatusBusyId(coupon.id);
      await updateCouponStatus(token, coupon.id, !coupon.isActive);
      setSuccessMessage(coupon.isActive ? "Cupom pausado." : "Cupom ativado.");
      setErrorMessage("");
      await loadCoupons(true);
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel alterar o cupom.");
    } finally {
      setStatusBusyId(null);
    }
  }

  return (
    <section className="module-body-grid single">
      <section className="surface-card zpcoupon-shell">
        <div className="zpcoupon-head">
          <div className="zpcoupon-head-copy">
            <span className="eyebrow">Cupons</span>
            <h2>Descontos da unidade</h2>
          </div>
          {!isFormOpen ? (
            <button className="zpcoupon-btn is-primary" type="button" onClick={openCreate}>
              Novo cupom
            </button>
          ) : null}
        </div>

        {isFormOpen ? (
          <form className="zpcoupon-form" onSubmit={handleSubmit}>
            <div className="zpcoupon-form-grid">
              <div className="field-group">
                <label htmlFor="couponCode">Codigo</label>
                <input
                  id="couponCode"
                  value={draft.code}
                  onChange={(event) => setDraft((current) => ({ ...current, code: event.target.value.toUpperCase() }))}
                  placeholder="EX: BEMVINDO10"
                  autoFocus
                />
              </div>

              <div className="field-group">
                <label htmlFor="couponType">Tipo de desconto</label>
                <select
                  id="couponType"
                  value={draft.discountType}
                  onChange={(event) => setDraft((current) => ({ ...current, discountType: event.target.value }))}
                >
                  <option value="Percent">Percentual (%)</option>
                  <option value="FixedAmount">Valor fixo (R$)</option>
                </select>
              </div>

              <div className="field-group">
                <label htmlFor="couponValue">{isPercent(draft.discountType) ? "Desconto (%)" : "Desconto (R$)"}</label>
                <input
                  id="couponValue"
                  inputMode="decimal"
                  value={draft.discountValue}
                  onChange={(event) => setDraft((current) => ({ ...current, discountValue: event.target.value }))}
                  placeholder={isPercent(draft.discountType) ? "10" : "15,00"}
                />
              </div>

              <div className="field-group">
                <label htmlFor="couponMinimum">Pedido minimo (R$)</label>
                <input
                  id="couponMinimum"
                  inputMode="decimal"
                  value={draft.minimumOrderAmount}
                  onChange={(event) => setDraft((current) => ({ ...current, minimumOrderAmount: event.target.value }))}
                  placeholder="0,00"
                />
              </div>

              <div className="field-group">
                <label htmlFor="couponStart">Inicio (opcional)</label>
                <input
                  id="couponStart"
                  type="date"
                  value={draft.startsAt}
                  onChange={(event) => setDraft((current) => ({ ...current, startsAt: event.target.value }))}
                />
              </div>

              <div className="field-group">
                <label htmlFor="couponEnd">Validade (opcional)</label>
                <input
                  id="couponEnd"
                  type="date"
                  value={draft.endsAt}
                  onChange={(event) => setDraft((current) => ({ ...current, endsAt: event.target.value }))}
                />
              </div>

              <div className="field-group">
                <label htmlFor="couponLimit">Limite de uso (opcional)</label>
                <input
                  id="couponLimit"
                  inputMode="numeric"
                  value={draft.usageLimit}
                  onChange={(event) => setDraft((current) => ({ ...current, usageLimit: event.target.value }))}
                  placeholder="Ilimitado"
                />
              </div>

              <div className="field-group zpcoupon-field-wide">
                <label htmlFor="couponDescription">Descricao (opcional)</label>
                <input
                  id="couponDescription"
                  value={draft.description}
                  onChange={(event) => setDraft((current) => ({ ...current, description: event.target.value }))}
                  placeholder="Ex.: desconto de boas-vindas"
                />
              </div>
            </div>

            <div className="zpcoupon-form-actions">
              <button className="zpcoupon-btn is-primary" type="submit" disabled={isSaving}>
                {isSaving ? "Salvando..." : draft.id ? "Salvar cupom" : "Criar cupom"}
              </button>
              <button className="zpcoupon-btn is-ghost" type="button" onClick={closeForm} disabled={isSaving}>
                Cancelar
              </button>
            </div>
          </form>
        ) : null}

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

        {loading ? (
          <p className="loading-state">Carregando cupons...</p>
        ) : sortedCoupons.length === 0 ? (
          <div className="zpcoupon-empty">
            <strong>Nenhum cupom ainda</strong>
            <p>Crie o primeiro cupom para oferecer desconto no pedido da unidade.</p>
            {!isFormOpen ? (
              <button className="zpcoupon-btn is-primary" type="button" onClick={openCreate}>
                Criar primeiro cupom
              </button>
            ) : null}
          </div>
        ) : (
          <div className="zpcoupon-list">
            {sortedCoupons.map((coupon) => {
              const status = getCouponStatus(coupon);
              const busy = statusBusyId === coupon.id;

              return (
                <article key={coupon.id} className="zpcoupon-card">
                  <div className="zpcoupon-card-head">
                    <div className="zpcoupon-card-id">
                      <strong>{coupon.code}</strong>
                      {coupon.description ? <span>{coupon.description}</span> : null}
                    </div>
                    <span className={`zpcoupon-status is-${status.tone}`}>{status.label}</span>
                  </div>

                  <div className="zpcoupon-card-metrics">
                    <div>
                      <strong>{formatDiscount(coupon)}</strong>
                      <span>desconto</span>
                    </div>
                    <div>
                      <strong>{coupon.minimumOrderAmount > 0 ? formatCurrency(coupon.minimumOrderAmount) : "Sem minimo"}</strong>
                      <span>pedido minimo</span>
                    </div>
                    <div>
                      <strong>{formatUsage(coupon)}</strong>
                      <span>uso</span>
                    </div>
                  </div>

                  <div className="zpcoupon-card-foot">
                    <span className="zpcoupon-validity">{formatValidity(coupon)}</span>
                    <div className="zpcoupon-card-actions">
                      <button className="zpcoupon-btn is-ghost" type="button" onClick={() => openEdit(coupon)} disabled={busy}>
                        Editar
                      </button>
                      <button
                        className={`zpcoupon-btn ${coupon.isActive ? "is-ghost" : "is-primary"}`}
                        type="button"
                        onClick={() => void handleToggleStatus(coupon)}
                        disabled={busy}
                      >
                        {busy ? "..." : coupon.isActive ? "Pausar" : "Ativar"}
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </section>
  );
}
