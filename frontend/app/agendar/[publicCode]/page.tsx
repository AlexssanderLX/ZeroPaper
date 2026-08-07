"use client";

import { FormEvent, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import styles from "./public-booking.module.css";
import {
  createPublicAppointmentRequest,
  getPublicAppointmentTracking,
  getPublicPetShop,
  getPublicPetShopAvailability,
  getPublicPetShopServices,
  cancelPublicAppointment,
  type AvailabilityDto,
  type PublicAppointmentCreatedDto,
  type PublicAppointmentTrackingDto,
  type PublicPetShopDto,
  type PublicServiceDto,
  type PetSpecies,
  type PetSize,
  ApiError,
} from "@/lib/api";

type Step =
  | "loading"
  | "error"
  | "establishment"
  | "services"
  | "customer"
  | "pet"
  | "datetime"
  | "notes"
  | "review"
  | "confirmed"
  | "tracking";

const TRACKING_KEY = "zp.petshop.access_token";

function escapeText(v: string) {
  return v.replace(/[<>&"']/g, (c) => ({ "<": "&lt;", ">": "&gt;", "&": "&amp;", '"': "&quot;", "'": "&#39;" })[c] ?? c);
}

function formatTimeUtc(utcStr: string, tz?: string) {
  try {
    return new Intl.DateTimeFormat("pt-BR", {
      hour: "2-digit",
      minute: "2-digit",
      timeZone: tz ?? "America/Sao_Paulo",
    }).format(new Date(utcStr));
  } catch {
    return utcStr.slice(11, 16);
  }
}

function formatDateUtc(utcStr: string, tz?: string) {
  try {
    return new Intl.DateTimeFormat("pt-BR", {
      weekday: "long",
      day: "2-digit",
      month: "long",
      timeZone: tz ?? "America/Sao_Paulo",
    }).format(new Date(utcStr));
  } catch {
    return utcStr.slice(0, 10);
  }
}

function localDateString(d: Date) {
  return d.toISOString().slice(0, 10);
}

const SPECIES_OPTIONS: { value: PetSpecies; label: string }[] = [
  { value: 1, label: "Cachorro" },
  { value: 2, label: "Gato" },
  { value: 3, label: "Outro" },
];

const SIZE_OPTIONS: { value: PetSize; label: string }[] = [
  { value: 1, label: "Pequeno" },
  { value: 2, label: "Medio" },
  { value: 3, label: "Grande" },
];

function statusLabel(s: number) {
  const map: Record<number, string> = {
    1: "Aguardando confirmacao",
    2: "Confirmado",
    3: "Em atendimento",
    4: "Concluido",
    5: "Cancelado",
    6: "Ausencia",
  };
  return map[s] ?? String(s);
}

export default function PublicBookingPage() {
  const params = useParams();
  const publicCode = typeof params.publicCode === "string" ? params.publicCode : "";

  const [step, setStep] = useState<Step>("loading");
  const [shop, setShop] = useState<PublicPetShopDto | null>(null);
  const [services, setServices] = useState<PublicServiceDto[]>([]);
  const [selectedService, setSelectedService] = useState<PublicServiceDto | null>(null);
  const [errorMessage, setErrorMessage] = useState("");

  // Customer form
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");

  // Pet form
  const [petName, setPetName] = useState("");
  const [petSpecies, setPetSpecies] = useState<PetSpecies>(1);
  const [petSize, setPetSize] = useState<PetSize>(1);
  const [petBreed, setPetBreed] = useState("");

  // Date/time
  const [selectedDate, setSelectedDate] = useState(localDateString(new Date()));
  const [availability, setAvailability] = useState<AvailabilityDto | null>(null);
  const [availabilityLoading, setAvailabilityLoading] = useState(false);
  const [selectedSlot, setSelectedSlot] = useState("");

  // Notes
  const [notes, setNotes] = useState("");

  // Confirmation
  const [created, setCreated] = useState<PublicAppointmentCreatedDto | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Tracking
  const [tracking, setTracking] = useState<PublicAppointmentTrackingDto | null>(null);
  const [trackingLoading, setTrackingLoading] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);

  useEffect(() => {
    if (!publicCode) {
      setStep("error");
      setErrorMessage("Codigo invalido.");
      return;
    }

    // Check for existing access token in sessionStorage
    try {
      const stored = sessionStorage.getItem(TRACKING_KEY);
      if (stored) {
        // Show tracking view with existing token
        setStep("tracking");
        void loadTracking(stored);
        return;
      }
    } catch {
      // Ignore storage errors
    }

    void (async () => {
      try {
        const [shopData, servicesData] = await Promise.all([
          getPublicPetShop(publicCode),
          getPublicPetShopServices(publicCode),
        ]);
        setShop(shopData);
        setServices(servicesData);
        setStep("services");
      } catch {
        setStep("error");
        setErrorMessage("Estabelecimento nao encontrado ou link invalido.");
      }
    })();
  }, [publicCode]);

  async function loadTracking(token: string) {
    setTrackingLoading(true);
    try {
      const data = await getPublicAppointmentTracking(token);
      setTracking(data);
    } catch {
      // Token expired or revoked — clear and restart
      try {
        sessionStorage.removeItem(TRACKING_KEY);
      } catch {
        // Ignore
      }
      setTracking(null);
      setStep("services");
      if (shop === null) {
        void (async () => {
          try {
            const [shopData, servicesData] = await Promise.all([
              getPublicPetShop(publicCode),
              getPublicPetShopServices(publicCode),
            ]);
            setShop(shopData);
            setServices(servicesData);
            setStep("services");
          } catch {
            setStep("error");
            setErrorMessage("Estabelecimento nao encontrado.");
          }
        })();
      }
    } finally {
      setTrackingLoading(false);
    }
  }

  async function loadAvailability() {
    if (!selectedService || !selectedDate) return;
    setAvailabilityLoading(true);
    setSelectedSlot("");
    setAvailability(null);
    try {
      const avail = await getPublicPetShopAvailability(publicCode, {
        date: selectedDate,
        serviceId: selectedService.id,
      });
      setAvailability(avail);
    } catch {
      setAvailability({ date: selectedDate, serviceId: selectedService.id, durationMinutes: 0, timeZone: shop?.timeZone ?? "", slots: [] });
    } finally {
      setAvailabilityLoading(false);
    }
  }

  useEffect(() => {
    if (step === "datetime" && selectedService && selectedDate) {
      void loadAvailability();
    }
  }, [selectedDate, step]);

  function selectService(svc: PublicServiceDto) {
    setSelectedService(svc);
    setStep("customer");
  }

  function handleCustomer(e: FormEvent) {
    e.preventDefault();
    setStep("pet");
  }

  function handlePet(e: FormEvent) {
    e.preventDefault();
    setStep("datetime");
    void loadAvailability();
  }

  function handleDatetime(e: FormEvent) {
    e.preventDefault();
    if (!selectedSlot) {
      setErrorMessage("Selecione um horario.");
      return;
    }
    setErrorMessage("");
    setStep("notes");
  }

  function handleNotes(e: FormEvent) {
    e.preventDefault();
    setStep("review");
  }

  async function handleSubmit() {
    if (!selectedService || !selectedSlot) return;
    setIsSubmitting(true);
    setErrorMessage("");
    try {
      const result = await createPublicAppointmentRequest(publicCode, {
        customerName: customerName.trim(),
        phoneNumber: customerPhone.trim(),
        petName: petName.trim(),
        petSpecies,
        petSize,
        petBreed: petBreed.trim() || null,
        serviceId: selectedService.id,
        startsAtUtc: selectedSlot,
        notes: notes.trim() || null,
      });
      setCreated(result);
      // Store token in sessionStorage only — never in localStorage or analytics
      try {
        sessionStorage.setItem(TRACKING_KEY, result.accessToken);
      } catch {
        // Ignore storage errors
      }
      setStep("confirmed");
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setErrorMessage("O horario foi ocupado por outro cliente. Por favor, escolha outro horario.");
        setSelectedSlot("");
        setStep("datetime");
        void loadAvailability();
      } else if (error instanceof ApiError && error.status === 429) {
        setErrorMessage("Muitas tentativas. Aguarde alguns instantes e tente novamente.");
      } else {
        setErrorMessage("Nao foi possivel enviar a solicitacao. Tente novamente.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleCancelAppointment() {
    if (!created && !tracking) return;
    if (!window.confirm("Tem certeza que deseja cancelar este agendamento?")) return;
    setIsCancelling(true);
    setErrorMessage("");
    try {
      let token: string | null = null;
      try {
        token = sessionStorage.getItem(TRACKING_KEY);
      } catch {
        // Ignore
      }
      if (!token) {
        setErrorMessage("Token de acesso nao encontrado.");
        return;
      }
      const updated = await cancelPublicAppointment(token);
      setTracking(updated);
      // Token is revoked after cancellation
      try {
        sessionStorage.removeItem(TRACKING_KEY);
      } catch {
        // Ignore
      }
    } catch {
      setErrorMessage("Nao foi possivel cancelar. Tente novamente ou entre em contato com o estabelecimento.");
    } finally {
      setIsCancelling(false);
    }
  }

  function formatCurrencyBRL(v: number) {
    return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(v);
  }

  const stepLabels: Partial<Record<Step, string>> = {
    establishment: "Estabelecimento",
    services: "Servico",
    customer: "Seus dados",
    pet: "Animal",
    datetime: "Data e horario",
    notes: "Observacoes",
    review: "Revisao",
    confirmed: "Confirmado",
    tracking: "Acompanhar",
  };

  const currentStepLabel = step !== "loading" && step !== "error" ? stepLabels[step] ?? "" : "";

  // ─── Loading ─────────────────────────────────────────────────────────────────
  if (step === "loading") {
    return (
      <main style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", padding: "2rem" }}>
        <p>Carregando...</p>
      </main>
    );
  }

  // ─── Error ───────────────────────────────────────────────────────────────────
  if (step === "error") {
    return (
      <main style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", padding: "2rem" }}>
        <div style={{ maxWidth: 480, textAlign: "center" }}>
          <h1>Nao encontrado</h1>
          <p style={{ marginTop: "1rem" }}>{errorMessage || "Link invalido ou expirado."}</p>
        </div>
      </main>
    );
  }

  const tz = shop?.timeZone;

  return (
    <main className={styles.page}>
      {/* Header */}
      {shop && (
        <header className={styles.header}>
          {shop.logoUrl && (
            <img
              src={shop.logoUrl}
              alt={shop.businessName}
              className={styles.logo}
            />
          )}
          <div><h1>{escapeText(shop.businessName)}</h1><small>Agendamento online</small></div>
          {currentStepLabel && (
            <p>{currentStepLabel}</p>
          )}
        </header>
      )}

      {errorMessage && (
        <div
          style={{
            background: "var(--color-error-bg, #fff0f0)",
            border: "1px solid var(--color-error, #cc0000)",
            borderRadius: 8,
            padding: "0.75rem 1rem",
            marginBottom: "1rem",
            color: "var(--color-error, #cc0000)",
          }}
        >
          {errorMessage}
        </div>
      )}

      {/* ─── Step: establishment ─────────────────────────────────────────── */}
      {step === "establishment" && (
        <div>
          <p style={{ marginBottom: "1.5rem" }}>
            Agende um horario com <strong>{escapeText(shop?.businessName ?? "")}</strong>. Clique em continuar para ver os servicos disponíveis.
          </p>
          <button
            type="button"
            onClick={() => setStep("services")}
            style={primaryBtnStyle}
          >
            Ver servicos disponíveis
          </button>
        </div>
      )}

      {/* ─── Step: services ──────────────────────────────────────────────── */}
      {step === "services" && (
        <section className={styles.catalog}>
          <div className={styles.catalogHeading}><span>Servicos</span><h2>O que seu pet precisa?</h2></div>
          {services.length === 0 && <p>Nenhum servico disponivel no momento.</p>}
          <ul className={styles.serviceList}>
            {services.map((svc) => (
              <li key={svc.id}>
                <button
                  type="button"
                  onClick={() => selectService(svc)}
                  className={styles.serviceCard}
                >
                  <span className={styles.serviceCopy}>
                    <strong>{escapeText(svc.name)}</strong>
                    {svc.description && <small>{escapeText(svc.description)}</small>}
                    <b>{formatCurrencyBRL(svc.price)} <em>{svc.durationMinutes} min</em></b>
                  </span>
                  {svc.imageUrl ? (
                    <img
                      src={svc.imageUrl}
                      alt={svc.name}
                      className={styles.serviceImage}
                    />
                  ) : <span className={styles.serviceImageFallback}>Pet</span>}
                </button>
              </li>
            ))}
          </ul>
        </section>
      )}

      {/* ─── Step: customer ──────────────────────────────────────────────── */}
      {step === "customer" && (
        <form onSubmit={handleCustomer} style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <h2>Seus dados</h2>
          <div style={fieldStyle}>
            <label htmlFor="pub-name">Seu nome</label>
            <input
              id="pub-name"
              type="text"
              required
              value={customerName}
              onChange={(e) => setCustomerName(e.target.value)}
              style={inputStyle}
            />
          </div>
          <div style={fieldStyle}>
            <label htmlFor="pub-phone">Seu telefone (WhatsApp)</label>
            <input
              id="pub-phone"
              type="tel"
              required
              value={customerPhone}
              onChange={(e) => setCustomerPhone(e.target.value)}
              placeholder="11999999999"
              style={inputStyle}
            />
          </div>
          <div style={{ display: "flex", gap: "0.75rem" }}>
            <button type="button" onClick={() => setStep("services")} style={secondaryBtnStyle}>
              Voltar
            </button>
            <button type="submit" style={primaryBtnStyle}>
              Continuar
            </button>
          </div>
        </form>
      )}

      {/* ─── Step: pet ───────────────────────────────────────────────────── */}
      {step === "pet" && (
        <form onSubmit={handlePet} style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <h2>Dados do animal</h2>
          <div style={fieldStyle}>
            <label htmlFor="pub-pet-name">Nome do animal</label>
            <input
              id="pub-pet-name"
              type="text"
              required
              value={petName}
              onChange={(e) => setPetName(e.target.value)}
              style={inputStyle}
            />
          </div>
          <div style={fieldStyle}>
            <label htmlFor="pub-pet-species">Especie</label>
            <select
              id="pub-pet-species"
              value={petSpecies}
              onChange={(e) => setPetSpecies(Number(e.target.value) as PetSpecies)}
              style={inputStyle}
            >
              {SPECIES_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>
          <div style={fieldStyle}>
            <label htmlFor="pub-pet-size">Porte</label>
            <select
              id="pub-pet-size"
              value={petSize}
              onChange={(e) => setPetSize(Number(e.target.value) as PetSize)}
              style={inputStyle}
            >
              {SIZE_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>
          <div style={fieldStyle}>
            <label htmlFor="pub-pet-breed">Raca (opcional)</label>
            <input
              id="pub-pet-breed"
              type="text"
              value={petBreed}
              onChange={(e) => setPetBreed(e.target.value)}
              placeholder="SRD"
              style={inputStyle}
            />
          </div>
          <div style={{ display: "flex", gap: "0.75rem" }}>
            <button type="button" onClick={() => setStep("customer")} style={secondaryBtnStyle}>
              Voltar
            </button>
            <button type="submit" style={primaryBtnStyle}>
              Continuar
            </button>
          </div>
        </form>
      )}

      {/* ─── Step: datetime ──────────────────────────────────────────────── */}
      {step === "datetime" && (
        <form onSubmit={handleDatetime} style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <h2>Escolha a data e o horario</h2>
          <div style={fieldStyle}>
            <label htmlFor="pub-date">Data</label>
            <input
              id="pub-date"
              type="date"
              required
              value={selectedDate}
              min={localDateString(new Date())}
              onChange={(e) => setSelectedDate(e.target.value)}
              style={inputStyle}
            />
          </div>

          {availabilityLoading && <p>Carregando horarios...</p>}

          {!availabilityLoading && availability && (
            <div style={fieldStyle}>
              <label>Horario disponivel</label>
              {availability.slots.length === 0 ? (
                <p>Sem horarios para esta data. Tente outro dia.</p>
              ) : (
                <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem" }}>
                  {availability.slots.map((slot) => (
                    <button
                      key={slot.startsAtUtc}
                      type="button"
                      onClick={() => setSelectedSlot(slot.startsAtUtc)}
                      style={{
                        padding: "0.5rem 0.75rem",
                        borderRadius: 8,
                        border: "1px solid",
                        borderColor: selectedSlot === slot.startsAtUtc ? "var(--color-primary, #007aff)" : "var(--color-border, #e0e0e0)",
                        background: selectedSlot === slot.startsAtUtc ? "var(--color-primary, #007aff)" : "transparent",
                        color: selectedSlot === slot.startsAtUtc ? "#fff" : "inherit",
                        cursor: "pointer",
                        fontWeight: 500,
                      }}
                    >
                      {formatTimeUtc(slot.startsAtUtc, tz)}
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}

          <div style={{ display: "flex", gap: "0.75rem" }}>
            <button type="button" onClick={() => setStep("pet")} style={secondaryBtnStyle}>
              Voltar
            </button>
            <button type="submit" disabled={!selectedSlot} style={primaryBtnStyle}>
              Continuar
            </button>
          </div>
        </form>
      )}

      {/* ─── Step: notes ─────────────────────────────────────────────────── */}
      {step === "notes" && (
        <form onSubmit={handleNotes} style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <h2>Observacoes (opcional)</h2>
          <div style={fieldStyle}>
            <label htmlFor="pub-notes">Alguma informacao importante?</label>
            <textarea
              id="pub-notes"
              rows={4}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Ex: animal assustado com barulhos..."
              style={{ ...inputStyle, resize: "vertical" }}
            />
          </div>
          <div style={{ display: "flex", gap: "0.75rem" }}>
            <button type="button" onClick={() => setStep("datetime")} style={secondaryBtnStyle}>
              Voltar
            </button>
            <button type="submit" style={primaryBtnStyle}>
              Revisar pedido
            </button>
          </div>
        </form>
      )}

      {/* ─── Step: review ────────────────────────────────────────────────── */}
      {step === "review" && selectedService && selectedSlot && (
        <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <h2>Revisao do pedido</h2>
          <div
            style={{
              background: "var(--color-surface-secondary, #f9f9f9)",
              borderRadius: 12,
              padding: "1.25rem",
              display: "flex",
              flexDirection: "column",
              gap: "0.5rem",
            }}
          >
            <p><strong>Estabelecimento:</strong> {escapeText(shop?.businessName ?? "")}</p>
            <p><strong>Servico:</strong> {escapeText(selectedService.name)}</p>
            <p><strong>Data:</strong> {formatDateUtc(selectedSlot, tz)}</p>
            <p><strong>Horario:</strong> {formatTimeUtc(selectedSlot, tz)}</p>
            <p><strong>Duracao:</strong> {selectedService.durationMinutes} min</p>
            <p><strong>Valor:</strong> {formatCurrencyBRL(selectedService.price)}</p>
            <hr style={{ border: "none", borderTop: "1px solid var(--color-border, #e0e0e0)" }} />
            <p><strong>Tutor:</strong> {escapeText(customerName)}</p>
            <p><strong>Telefone:</strong> {customerPhone}</p>
            <p><strong>Animal:</strong> {escapeText(petName)}</p>
            {notes && <p><strong>Observacoes:</strong> {escapeText(notes)}</p>}
          </div>
          {errorMessage && (
            <p style={{ color: "var(--color-error, #cc0000)" }}>{errorMessage}</p>
          )}
          <div style={{ display: "flex", gap: "0.75rem" }}>
            <button type="button" onClick={() => setStep("notes")} style={secondaryBtnStyle}>
              Voltar
            </button>
            <button type="button" disabled={isSubmitting} onClick={() => void handleSubmit()} style={primaryBtnStyle}>
              {isSubmitting ? "Enviando..." : "Confirmar solicitacao"}
            </button>
          </div>
        </div>
      )}

      {/* ─── Step: confirmed ─────────────────────────────────────────────── */}
      {step === "confirmed" && created && selectedService && (
        <div style={{ textAlign: "center" }}>
          <div style={{ fontSize: "3rem", marginBottom: "1rem" }}>✓</div>
          <h2>Solicitacao enviada!</h2>
          <p style={{ marginTop: "0.75rem", color: "var(--color-muted, #666)" }}>
            Voce sera contatado pelo estabelecimento para confirmar o agendamento.
          </p>
          <div
            style={{
              background: "var(--color-surface-secondary, #f9f9f9)",
              borderRadius: 12,
              padding: "1.25rem",
              margin: "1.5rem 0",
              textAlign: "left",
            }}
          >
            <p><strong>Status:</strong> {statusLabel(created.status)}</p>
            <p><strong>Servico:</strong> {escapeText(selectedService.name)}</p>
            <p><strong>Horario:</strong> {formatDateUtc(created.startsAtUtc, tz)}, {formatTimeUtc(created.startsAtUtc, tz)}</p>
          </div>
          <p style={{ fontSize: "0.875em", color: "var(--color-muted, #666)", marginBottom: "1.5rem" }}>
            Salve esta pagina para acompanhar o status do agendamento.
          </p>
          <button
            type="button"
            onClick={() => {
              setStep("tracking");
              void loadTracking(created.accessToken);
            }}
            style={primaryBtnStyle}
          >
            Acompanhar agendamento
          </button>
        </div>
      )}

      {/* ─── Step: tracking ──────────────────────────────────────────────── */}
      {step === "tracking" && (
        <div>
          <h2>Acompanhamento</h2>
          {trackingLoading && <p style={{ marginTop: "1rem" }}>Carregando...</p>}
          {errorMessage && <p style={{ color: "var(--color-error, #cc0000)", marginTop: "1rem" }}>{errorMessage}</p>}
          {!trackingLoading && tracking && (
            <div
              style={{
                background: "var(--color-surface-secondary, #f9f9f9)",
                borderRadius: 12,
                padding: "1.25rem",
                marginTop: "1rem",
                display: "flex",
                flexDirection: "column",
                gap: "0.5rem",
              }}
            >
              <p><strong>Animal:</strong> {escapeText(tracking.petName)}</p>
              <p><strong>Servico:</strong> {escapeText(tracking.serviceName)}</p>
              <p><strong>Data:</strong> {formatDateUtc(tracking.startsAtUtc, tz)}</p>
              <p><strong>Horario:</strong> {formatTimeUtc(tracking.startsAtUtc, tz)}</p>
              <p>
                <strong>Status:</strong>{" "}
                <span style={{ fontWeight: 700 }}>{statusLabel(tracking.status)}</span>
              </p>

              {tracking.canCancel && (
                <div style={{ marginTop: "1rem" }}>
                  <button
                    type="button"
                    disabled={isCancelling}
                    onClick={() => void handleCancelAppointment()}
                    style={{ ...secondaryBtnStyle, color: "var(--color-error, #cc0000)", borderColor: "var(--color-error, #cc0000)" }}
                  >
                    {isCancelling ? "Cancelando..." : "Cancelar agendamento"}
                  </button>
                </div>
              )}

              {(tracking.status === 5 || tracking.status === 4 || tracking.status === 6) && (
                <div style={{ marginTop: "1rem" }}>
                  <button
                    type="button"
                    onClick={() => {
                      try { sessionStorage.removeItem(TRACKING_KEY); } catch { /* Ignore */ }
                      setTracking(null);
                      setStep("services");
                      setSelectedService(null);
                      setSelectedSlot("");
                      setSelectedDate(localDateString(new Date()));
                      setCustomerName("");
                      setCustomerPhone("");
                      setPetName("");
                      setPetBreed("");
                      setNotes("");
                    }}
                    style={secondaryBtnStyle}
                  >
                    Novo agendamento
                  </button>
                </div>
              )}
            </div>
          )}
          {!trackingLoading && !tracking && (
            <div style={{ marginTop: "1rem" }}>
              <p>Token expirado ou agendamento nao encontrado.</p>
              <button
                type="button"
                style={{ ...secondaryBtnStyle, marginTop: "1rem" }}
                onClick={() => {
                  try { sessionStorage.removeItem(TRACKING_KEY); } catch { /* Ignore */ }
                  setStep("services");
                }}
              >
                Fazer novo agendamento
              </button>
            </div>
          )}
        </div>
      )}
    </main>
  );
}

// Inline styles for the standalone public page (no workspace CSS classes available)
const primaryBtnStyle: React.CSSProperties = {
  padding: "0.75rem 1.5rem",
  borderRadius: 10,
  background: "var(--color-primary, #007aff)",
  color: "#fff",
  border: "none",
  cursor: "pointer",
  fontWeight: 600,
  fontSize: "1rem",
  flex: 1,
};

const secondaryBtnStyle: React.CSSProperties = {
  padding: "0.75rem 1.5rem",
  borderRadius: 10,
  background: "transparent",
  border: "1px solid var(--color-border, #ccc)",
  cursor: "pointer",
  fontWeight: 500,
  fontSize: "1rem",
};

const fieldStyle: React.CSSProperties = {
  display: "flex",
  flexDirection: "column",
  gap: "0.25rem",
};

const inputStyle: React.CSSProperties = {
  padding: "0.625rem 0.75rem",
  borderRadius: 8,
  border: "1px solid var(--color-border, #e0e0e0)",
  fontSize: "1rem",
  width: "100%",
  boxSizing: "border-box",
};
