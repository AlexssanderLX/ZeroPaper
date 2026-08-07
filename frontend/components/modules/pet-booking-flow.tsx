"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  ApiError,
  createAppointment,
  createPet,
  createPetShopCustomer,
  getAppointmentAvailability,
  getCatalogServices,
  getPetShopCustomers,
  getPets,
  getProfessionals,
  type AppointmentDto,
  type AvailabilityDto,
  type CatalogServiceDto,
  type CustomerProfileDto,
  type PetDto,
  type PetSize,
  type PetSpecies,
  type ProfessionalDto,
} from "@/lib/api";
import { formatCurrency, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";
import styles from "./pet-booking-flow.module.css";

type Step = "customer" | "pet" | "service" | "professional" | "schedule" | "confirm";

const steps: { key: Step; label: string }[] = [
  { key: "customer", label: "Tutor" },
  { key: "pet", label: "Animal" },
  { key: "service", label: "Servico" },
  { key: "professional", label: "Profissional" },
  { key: "schedule", label: "Horario" },
  { key: "confirm", label: "Confirmar" },
];

function localDate(offset = 0) {
  const date = new Date();
  date.setDate(date.getDate() + offset);
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
}

function formatTime(value: string, timeZone?: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    hour: "2-digit",
    minute: "2-digit",
    timeZone: timeZone ?? "America/Sao_Paulo",
  }).format(new Date(value));
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric" })
    .format(new Date(`${value}T12:00:00`));
}

export function PetBookingFlow({
  token,
  onUnauthorized,
  initialDate,
  onCancel,
  onCreated,
}: {
  token: string;
  onUnauthorized: AsyncVoid;
  initialDate: string;
  onCancel: () => void;
  onCreated: (appointment: AppointmentDto) => Promise<void>;
}) {
  const [step, setStep] = useState<Step>("customer");
  const [customers, setCustomers] = useState<CustomerProfileDto[]>([]);
  const [pets, setPets] = useState<PetDto[]>([]);
  const [services, setServices] = useState<CatalogServiceDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [customer, setCustomer] = useState<CustomerProfileDto | null>(null);
  const [pet, setPet] = useState<PetDto | null>(null);
  const [service, setService] = useState<CatalogServiceDto | null>(null);
  const [professional, setProfessional] = useState<ProfessionalDto | null>(null);
  const [date, setDate] = useState(initialDate || localDate());
  const [slot, setSlot] = useState("");
  const [availability, setAvailability] = useState<AvailabilityDto | null>(null);
  const [search, setSearch] = useState("");
  const [notes, setNotes] = useState("");
  const [loading, setLoading] = useState(true);
  const [availabilityLoading, setAvailabilityLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [showNewCustomer, setShowNewCustomer] = useState(false);
  const [showNewPet, setShowNewPet] = useState(false);
  const [newCustomerName, setNewCustomerName] = useState("");
  const [newCustomerPhone, setNewCustomerPhone] = useState("");
  const [newPetName, setNewPetName] = useState("");
  const [newPetSpecies, setNewPetSpecies] = useState<PetSpecies>(1);
  const [newPetSize, setNewPetSize] = useState<PetSize>(1);

  useEffect(() => {
    let active = true;
    void Promise.all([
      getPetShopCustomers(token, { pageSize: 200 }),
      getCatalogServices(token),
      getProfessionals(token),
    ]).then(([customerResult, serviceResult, professionalResult]) => {
      if (!active) return;
      setCustomers(customerResult.items);
      setServices(serviceResult.filter((item) => item.isPublished && item.estimatedDurationMinutes));
      setProfessionals(professionalResult);
    }).catch((loadError) => {
      void handleApiError(loadError, onUnauthorized, setError, "Nao foi possivel abrir o agendamento rapido.");
    }).finally(() => active && setLoading(false));
    return () => { active = false; };
  }, [token, onUnauthorized]);

  useEffect(() => {
    if (!service || !date || step !== "schedule") return;
    setAvailabilityLoading(true);
    setSlot("");
    setAvailability(null);
    void getAppointmentAvailability(token, {
      date,
      serviceId: service.id,
      assignedUserId: professional?.id,
    }).then(setAvailability).catch((loadError) => {
      void handleApiError(loadError, onUnauthorized, setError, "Nao foi possivel carregar os horarios.");
    }).finally(() => setAvailabilityLoading(false));
  }, [date, professional, service, step, token, onUnauthorized]);

  const filteredCustomers = useMemo(() => {
    const query = search.trim().toLocaleLowerCase("pt-BR");
    if (!query) return customers;
    return customers.filter((item) => `${item.name} ${item.phoneNumber}`.toLocaleLowerCase("pt-BR").includes(query));
  }, [customers, search]);

  async function selectCustomer(selected: CustomerProfileDto) {
    setCustomer(selected);
    setPet(null);
    setError("");
    try {
      const result = await getPets(token, { customerProfileId: selected.id, isActive: true, pageSize: 50 });
      setPets(result.items);
      if (result.items.length === 1) {
        setPet(result.items[0]);
        setStep("service");
      } else {
        setStep("pet");
      }
    } catch (loadError) {
      await handleApiError(loadError, onUnauthorized, setError, "Nao foi possivel carregar os animais.");
    }
  }

  async function createCustomer(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError("");
    try {
      const created = await createPetShopCustomer(token, {
        name: newCustomerName.trim(),
        phoneNumber: newCustomerPhone.trim(),
      });
      setCustomers((current) => [created, ...current]);
      setShowNewCustomer(false);
      await selectCustomer(created);
    } catch (createError) {
      await handleApiError(createError, onUnauthorized, setError, "Nao foi possivel cadastrar o tutor.");
    } finally {
      setSaving(false);
    }
  }

  async function createNewPet(event: FormEvent) {
    event.preventDefault();
    if (!customer) return;
    setSaving(true);
    setError("");
    try {
      const created = await createPet(token, {
        customerProfileId: customer.id,
        name: newPetName.trim(),
        species: newPetSpecies,
        size: newPetSize,
      });
      setPets((current) => [created, ...current]);
      setPet(created);
      setShowNewPet(false);
      setStep("service");
    } catch (createError) {
      await handleApiError(createError, onUnauthorized, setError, "Nao foi possivel cadastrar o animal.");
    } finally {
      setSaving(false);
    }
  }

  async function confirm() {
    if (!pet || !service || !slot) return;
    setSaving(true);
    setError("");
    try {
      const created = await createAppointment(token, {
        petId: pet.id,
        menuItemId: service.id,
        assignedUserId: professional?.id ?? null,
        startsAtUtc: slot,
        customerNotes: notes.trim() || null,
      });
      await onCreated(created);
    } catch (createError) {
      if (createError instanceof ApiError && createError.status === 409) {
        setError("Este horario acabou de ficar indisponivel. Escolha outro.");
        setStep("schedule");
        setSlot("");
      } else {
        await handleApiError(createError, onUnauthorized, setError, "Nao foi possivel criar o agendamento.");
      }
    } finally {
      setSaving(false);
    }
  }

  const stepIndex = steps.findIndex((item) => item.key === step);

  return (
    <section className={`${styles.flow} surface-card`} aria-labelledby="booking-flow-title">
      <header className={styles.header}>
        <div>
          <span className={styles.eyebrow}>Agendamento rapido</span>
          <h1 id="booking-flow-title">Novo agendamento</h1>
          <p>Escolha com cliques. O servidor confirma preco, duracao e disponibilidade.</p>
        </div>
        <button type="button" className="secondary-button" onClick={onCancel}>Fechar</button>
      </header>

      <ol className={styles.progress} aria-label="Etapas do agendamento">
        {steps.map((item, index) => (
          <li key={item.key} className={index === stepIndex ? styles.current : index < stepIndex ? styles.done : ""}>
            <span>{index + 1}</span>{item.label}
          </li>
        ))}
      </ol>

      {error && <p className="error-message">{error}</p>}
      {loading && <p className="workspace-inline-loading">Preparando o fluxo...</p>}

      {!loading && step === "customer" && (
        <div className={styles.stage}>
          <div className={styles.stageTitle}><div><span>1 de 6</span><h2>Quem e o tutor?</h2></div></div>
          <input className={styles.search} type="search" placeholder="Buscar nome ou telefone" value={search} onChange={(event) => setSearch(event.target.value)} autoFocus />
          <div className={styles.choiceGrid}>
            {filteredCustomers.map((item) => (
              <button key={item.id} type="button" className={styles.personCard} onClick={() => void selectCustomer(item)}>
                <span className={styles.avatar}>{item.name.slice(0, 1).toUpperCase()}</span>
                <span><strong>{item.name}</strong><small>{item.phoneNumber}</small></span>
              </button>
            ))}
            <button type="button" className={styles.addCard} onClick={() => setShowNewCustomer(true)}>+ Novo tutor</button>
          </div>
          {showNewCustomer && (
            <form className={styles.quickForm} onSubmit={createCustomer}>
              <h3>Cadastrar tutor sem sair</h3>
              <input required placeholder="Nome" value={newCustomerName} onChange={(e) => setNewCustomerName(e.target.value)} />
              <input required type="tel" placeholder="Telefone" value={newCustomerPhone} onChange={(e) => setNewCustomerPhone(e.target.value)} />
              <div><button className="primary-button" disabled={saving}>Salvar e continuar</button><button type="button" className="secondary-button" onClick={() => setShowNewCustomer(false)}>Cancelar</button></div>
            </form>
          )}
        </div>
      )}

      {!loading && step === "pet" && customer && (
        <div className={styles.stage}>
          <button type="button" className={styles.back} onClick={() => setStep("customer")}>← Trocar tutor</button>
          <div className={styles.stageTitle}><div><span>2 de 6 · {customer.name}</span><h2>Qual animal sera atendido?</h2></div></div>
          <div className={styles.choiceGrid}>
            {pets.map((item) => (
              <button key={item.id} type="button" className={styles.petCard} onClick={() => { setPet(item); setStep("service"); }}>
                {item.photoUrl ? <img src={item.photoUrl} alt="" /> : <span className={styles.petIcon}>🐾</span>}
                <span><strong>{item.name}</strong><small>{item.breed || (item.species === 1 ? "Cachorro" : item.species === 2 ? "Gato" : "Outro")}</small></span>
              </button>
            ))}
            <button type="button" className={styles.addCard} onClick={() => setShowNewPet(true)}>+ Novo animal</button>
          </div>
          {showNewPet && (
            <form className={styles.quickForm} onSubmit={createNewPet}>
              <h3>Cadastrar animal sem sair</h3>
              <input required placeholder="Nome do animal" value={newPetName} onChange={(e) => setNewPetName(e.target.value)} />
              <div className={styles.chips}>{([1, 2, 3] as PetSpecies[]).map((value) => <button key={value} type="button" className={newPetSpecies === value ? styles.selectedChip : ""} onClick={() => setNewPetSpecies(value)}>{value === 1 ? "Cachorro" : value === 2 ? "Gato" : "Outro"}</button>)}</div>
              <div className={styles.chips}>{([1, 2, 3] as PetSize[]).map((value) => <button key={value} type="button" className={newPetSize === value ? styles.selectedChip : ""} onClick={() => setNewPetSize(value)}>{value === 1 ? "Pequeno" : value === 2 ? "Medio" : "Grande"}</button>)}</div>
              <div><button className="primary-button" disabled={saving}>Salvar e continuar</button><button type="button" className="secondary-button" onClick={() => setShowNewPet(false)}>Cancelar</button></div>
            </form>
          )}
        </div>
      )}

      {!loading && step === "service" && pet && (
        <div className={styles.stage}>
          <button type="button" className={styles.back} onClick={() => setStep("pet")}>← Trocar animal</button>
          <div className={styles.stageTitle}><div><span>3 de 6 · {pet.name}</span><h2>Escolha o servico</h2></div></div>
          <div className={styles.serviceGrid}>
            {services.map((item) => (
              <button key={item.id} type="button" className={styles.serviceCard} onClick={() => { setService(item); setStep("professional"); }}>
                {item.imageUrl ? <img src={item.imageUrl} alt="" /> : <span className={styles.serviceFallback}>✦</span>}
                <span className={styles.serviceBody}><strong>{item.name}</strong>{item.description && <small>{item.description}</small>}<span><b>{formatCurrency(item.price)}</b><em>{item.estimatedDurationMinutes} min</em></span></span>
              </button>
            ))}
          </div>
          {services.length === 0 && <p className="body-copy">Cadastre e publique um servico com duracao para agendar.</p>}
        </div>
      )}

      {!loading && step === "professional" && service && (
        <div className={styles.stage}>
          <button type="button" className={styles.back} onClick={() => setStep("service")}>← Trocar servico</button>
          <div className={styles.stageTitle}><div><span>4 de 6 · {service.name}</span><h2>Quem vai atender?</h2></div></div>
          <div className={styles.choiceGrid}>
            <button type="button" className={styles.personCard} onClick={() => { setProfessional(null); setStep("schedule"); }}><span className={styles.avatar}>★</span><span><strong>Qualquer profissional</strong><small>Definir depois</small></span></button>
            {professionals.map((item) => <button key={item.id} type="button" className={styles.personCard} onClick={() => { setProfessional(item); setStep("schedule"); }}><span className={styles.avatar}>{item.name.slice(0, 1)}</span><span><strong>{item.name}</strong><small>Selecionar</small></span></button>)}
          </div>
        </div>
      )}

      {!loading && step === "schedule" && service && (
        <div className={styles.stage}>
          <button type="button" className={styles.back} onClick={() => setStep("professional")}>← Trocar profissional</button>
          <div className={styles.stageTitle}><div><span>5 de 6</span><h2>Escolha data e horario</h2></div></div>
          <div className={styles.dateChoices}>
            <button type="button" className={date === localDate() ? styles.activeDate : ""} onClick={() => setDate(localDate())}>Hoje<small>{formatDate(localDate())}</small></button>
            <button type="button" className={date === localDate(1) ? styles.activeDate : ""} onClick={() => setDate(localDate(1))}>Amanha<small>{formatDate(localDate(1))}</small></button>
            <label>Outra data<input type="date" min={localDate()} value={date} onChange={(e) => setDate(e.target.value)} /></label>
          </div>
          {availabilityLoading && <p className="workspace-inline-loading">Consultando disponibilidade real...</p>}
          {!availabilityLoading && availability && (
            <div className={styles.slots}>
              {availability.slots.map((item) => <button key={item.startsAtUtc} type="button" className={slot === item.startsAtUtc ? styles.selectedSlot : ""} onClick={() => setSlot(item.startsAtUtc)}>{formatTime(item.startsAtUtc, availability.timeZone)}</button>)}
              {availability.slots.length === 0 && <p>Nenhum horario disponivel nesta data.</p>}
            </div>
          )}
          <div className={styles.stickyAction}><span>{slot ? `${formatDate(date)} às ${formatTime(slot, availability?.timeZone)}` : "Selecione um horario"}</span><button type="button" className="primary-button" disabled={!slot} onClick={() => setStep("confirm")}>Revisar</button></div>
        </div>
      )}

      {!loading && step === "confirm" && customer && pet && service && slot && (
        <div className={styles.stage}>
          <button type="button" className={styles.back} onClick={() => setStep("schedule")}>← Trocar horario</button>
          <div className={styles.stageTitle}><div><span>6 de 6</span><h2>Confirme o agendamento</h2></div></div>
          <dl className={styles.summary}>
            <div><dt>Tutor</dt><dd>{customer.name}</dd></div><div><dt>Animal</dt><dd>{pet.name}</dd></div>
            <div><dt>Servico</dt><dd>{service.name}</dd></div><div><dt>Profissional</dt><dd>{professional?.name ?? "A definir"}</dd></div>
            <div><dt>Data e horario</dt><dd>{formatDate(date)} · {formatTime(slot, availability?.timeZone)}</dd></div><div><dt>Valor</dt><dd>{formatCurrency(service.price)}</dd></div>
          </dl>
          <label className={styles.notes}>Observacao opcional<textarea rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Alergia, comportamento ou recado importante" /></label>
          <div className={styles.stickyAction}><span>O backend validara tudo novamente.</span><button type="button" className="primary-button" disabled={saving} onClick={() => void confirm()}>{saving ? "Confirmando..." : "Confirmar agendamento"}</button></div>
        </div>
      )}
    </section>
  );
}
