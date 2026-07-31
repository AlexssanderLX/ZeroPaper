"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  createPetShopCustomer,
  getPetShopCustomer,
  getPetShopCustomers,
  getPets,
  updatePetShopCustomer,
  type CustomerProfileDto,
  type PetDto,
} from "@/lib/api";
import { formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

type View = "list" | "detail" | "create" | "edit";

function escapeText(value: string) {
  return value.replace(/[<>&"']/g, (c) =>
    ({ "<": "&lt;", ">": "&gt;", "&": "&amp;", '"': "&quot;", "'": "&#39;" })[c] ?? c,
  );
}

export function PetCustomersModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [view, setView] = useState<View>("list");
  const [customers, setCustomers] = useState<CustomerProfileDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [searchInput, setSearchInput] = useState("");
  const [loading, setLoading] = useState(true);
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerProfileDto | null>(null);
  const [customerPets, setCustomerPets] = useState<PetDto[]>([]);
  const [petsLoading, setPetsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  // Create form
  const [cPhone, setCPhone] = useState("");
  const [cName, setCName] = useState("");
  const [cZip, setCZip] = useState("");
  const [cStreet, setCStreet] = useState("");
  const [cNumber, setCNumber] = useState("");
  const [cNeighborhood, setCNeighborhood] = useState("");
  const [cComplement, setCComplement] = useState("");

  const pageSize = 25;

  async function loadCustomers(p = page, s = search) {
    setLoading(true);
    try {
      const res = await getPetShopCustomers(token, { search: s, page: p, pageSize });
      setCustomers(res.items);
      setTotal(res.total);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os tutores.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadCustomers();
  }, [token, page, search]);

  async function handleSearch(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    setSearch(searchInput);
  }

  async function handleSelectCustomer(customer: CustomerProfileDto) {
    setSelectedCustomer(customer);
    setView("detail");
    setPetsLoading(true);
    setCustomerPets([]);
    try {
      const res = await getPets(token, { customerProfileId: customer.id, pageSize: 50 });
      setCustomerPets(res.items);
    } catch {
      // Non-critical; show empty list
    } finally {
      setPetsLoading(false);
    }
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setIsSaving(true);
    setErrorMessage("");
    try {
      await createPetShopCustomer(token, {
        phoneNumber: cPhone.trim(),
        name: cName.trim(),
        zipCode: cZip.trim() || null,
        street: cStreet.trim() || null,
        number: cNumber.trim() || null,
        neighborhood: cNeighborhood.trim() || null,
        complement: cComplement.trim() || null,
      });
      setSuccessMessage("Tutor cadastrado.");
      resetCreateForm();
      setView("list");
      setPage(1);
      await loadCustomers(1, search);
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel cadastrar o tutor.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleEdit(e: FormEvent) {
    e.preventDefault();
    if (!selectedCustomer) return;
    setIsSaving(true);
    setErrorMessage("");
    try {
      const updated = await updatePetShopCustomer(token, selectedCustomer.id, {
        name: cName.trim(),
        zipCode: cZip.trim() || null,
        street: cStreet.trim() || null,
        number: cNumber.trim() || null,
        neighborhood: cNeighborhood.trim() || null,
        complement: cComplement.trim() || null,
      });
      setSelectedCustomer(updated);
      setSuccessMessage("Tutor atualizado.");
      setView("detail");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o tutor.");
    } finally {
      setIsSaving(false);
    }
  }

  function openEdit(customer: CustomerProfileDto) {
    setSelectedCustomer(customer);
    setCName(customer.name);
    setCZip(customer.zipCode ?? "");
    setCStreet(customer.street ?? "");
    setCNumber(customer.number ?? "");
    setCNeighborhood(customer.neighborhood ?? "");
    setCComplement(customer.complement ?? "");
    setErrorMessage("");
    setView("edit");
  }

  function openCreate() {
    resetCreateForm();
    setErrorMessage("");
    setView("create");
  }

  function resetCreateForm() {
    setCPhone("");
    setCName("");
    setCZip("");
    setCStreet("");
    setCNumber("");
    setCNeighborhood("");
    setCComplement("");
  }

  function formatPetSpecies(s: number) {
    return s === 1 ? "Cachorro" : s === 2 ? "Gato" : "Outro";
  }

  function formatPetSize(s: number) {
    return s === 1 ? "Pequeno" : s === 2 ? "Medio" : "Grande";
  }

  const totalPages = Math.ceil(total / pageSize);

  if (view === "create" || view === "edit") {
    const isEdit = view === "edit";
    return (
      <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <h1>{isEdit ? "Editar tutor" : "Novo tutor"}</h1>
          </div>
          <div className="toolbar-actions compact">
            <button
              type="button"
              className="secondary-button"
              onClick={() => {
                setErrorMessage("");
                setView(isEdit ? "detail" : "list");
              }}
            >
              Cancelar
            </button>
          </div>
        </div>

        {errorMessage && <p className="error-message">{errorMessage}</p>}

        <form onSubmit={isEdit ? handleEdit : handleCreate} className="settings-form">
          {!isEdit && (
            <div className="form-field">
              <label htmlFor="ps-c-phone">Telefone</label>
              <input
                id="ps-c-phone"
                type="tel"
                required
                value={cPhone}
                onChange={(e) => setCPhone(e.target.value)}
                placeholder="11999999999"
              />
            </div>
          )}
          <div className="form-field">
            <label htmlFor="ps-c-name">Nome</label>
            <input
              id="ps-c-name"
              type="text"
              required
              value={cName}
              onChange={(e) => setCName(e.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="ps-c-zip">CEP</label>
            <input id="ps-c-zip" type="text" value={cZip} onChange={(e) => setCZip(e.target.value)} />
          </div>
          <div className="form-field">
            <label htmlFor="ps-c-street">Rua</label>
            <input id="ps-c-street" type="text" value={cStreet} onChange={(e) => setCStreet(e.target.value)} />
          </div>
          <div className="form-field">
            <label htmlFor="ps-c-number">Numero</label>
            <input id="ps-c-number" type="text" value={cNumber} onChange={(e) => setCNumber(e.target.value)} />
          </div>
          <div className="form-field">
            <label htmlFor="ps-c-neighborhood">Bairro</label>
            <input
              id="ps-c-neighborhood"
              type="text"
              value={cNeighborhood}
              onChange={(e) => setCNeighborhood(e.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="ps-c-complement">Complemento</label>
            <input
              id="ps-c-complement"
              type="text"
              value={cComplement}
              onChange={(e) => setCComplement(e.target.value)}
            />
          </div>
          <div className="toolbar-actions">
            <button type="submit" className="primary-button" disabled={isSaving}>
              {isSaving ? "Salvando..." : "Salvar"}
            </button>
          </div>
        </form>
      </section>
    );
  }

  if (view === "detail" && selectedCustomer) {
    return (
      <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <h1>{escapeText(selectedCustomer.name)}</h1>
            <p className="body-copy">{selectedCustomer.phoneNumber}</p>
          </div>
          <div className="toolbar-actions compact">
            <button type="button" className="secondary-button" onClick={() => setView("list")}>
              Voltar
            </button>
            <button type="button" className="primary-button" onClick={() => openEdit(selectedCustomer)}>
              Editar
            </button>
          </div>
        </div>

        {successMessage && <p className="success-message">{successMessage}</p>}

        <div className="settings-form">
          <p>
            <strong>Telefone:</strong> {selectedCustomer.phoneNumber}
          </p>
          {selectedCustomer.street && (
            <p>
              <strong>Endereco:</strong>{" "}
              {escapeText(
                [selectedCustomer.street, selectedCustomer.number, selectedCustomer.neighborhood, selectedCustomer.zipCode]
                  .filter(Boolean)
                  .join(", "),
              )}
            </p>
          )}
          {selectedCustomer.lastOrderAtUtc && (
            <p>
              <strong>Ultimo pedido:</strong> {formatDateTime(selectedCustomer.lastOrderAtUtc)}
            </p>
          )}
          <p>
            <strong>Cadastrado em:</strong> {formatDateTime(selectedCustomer.createdAtUtc)}
          </p>
        </div>

        <h2 style={{ marginTop: "1.5rem" }}>Animais</h2>
        {petsLoading && <p className="workspace-inline-loading">Carregando animais...</p>}
        {!petsLoading && customerPets.length === 0 && <p className="body-copy">Nenhum animal cadastrado.</p>}
        {!petsLoading && customerPets.length > 0 && (
          <ul className="simple-list">
            {customerPets.map((pet) => (
              <li key={pet.id} className="simple-list-item">
                <strong>{escapeText(pet.name)}</strong> — {formatPetSpecies(pet.species)},{" "}
                {formatPetSize(pet.size)}
                {pet.breed ? ` (${escapeText(pet.breed)})` : ""}
                {!pet.isActive && <span className="badge badge-inactive"> Inativo</span>}
              </li>
            ))}
          </ul>
        )}
      </section>
    );
  }

  return (
    <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
      <div className="workspace-summary-head">
        <div className="hero-stack">
          <h1>Tutores</h1>
          {total > 0 && <p className="body-copy">{total} tutor(es) cadastrado(s)</p>}
        </div>
        <div className="toolbar-actions compact">
          <button type="button" className="primary-button" onClick={openCreate}>
            Novo tutor
          </button>
        </div>
      </div>

      {errorMessage && <p className="error-message">{errorMessage}</p>}
      {successMessage && <p className="success-message">{successMessage}</p>}

      <form onSubmit={handleSearch} className="toolbar-actions compact" style={{ marginBottom: "1rem" }}>
        <input
          type="search"
          placeholder="Buscar por nome ou telefone..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          style={{ flex: 1 }}
        />
        <button type="submit" className="secondary-button">
          Buscar
        </button>
      </form>

      {loading && <p className="workspace-inline-loading">Carregando...</p>}

      {!loading && customers.length === 0 && (
        <p className="body-copy">Nenhum tutor encontrado{search ? " para essa busca" : ""}.</p>
      )}

      {!loading && customers.length > 0 && (
        <ul className="simple-list">
          {customers.map((c) => (
            <li key={c.id} className="simple-list-item simple-list-item--clickable" onClick={() => void handleSelectCustomer(c)}>
              <div>
                <strong>{escapeText(c.name)}</strong>
                <br />
                <span className="body-copy">{c.phoneNumber}</span>
              </div>
            </li>
          ))}
        </ul>
      )}

      {totalPages > 1 && (
        <div className="toolbar-actions compact" style={{ marginTop: "1rem", justifyContent: "center" }}>
          <button
            type="button"
            className="secondary-button"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            Anterior
          </button>
          <span>
            {page} / {totalPages}
          </span>
          <button
            type="button"
            className="secondary-button"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            Proxima
          </button>
        </div>
      )}
    </section>
  );
}
