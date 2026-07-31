"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import {
  createPet,
  deletePetPhoto,
  getPetShopCustomers,
  getPets,
  updatePet,
  updatePetStatus,
  uploadPetPhoto,
  type CustomerProfileDto,
  type PetDto,
  type PetSize,
  type PetSpecies,
} from "@/lib/api";
import { formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

type View = "list" | "detail" | "create" | "edit";

function escapeText(v: string) {
  return v.replace(/[<>&"']/g, (c) => ({ "<": "&lt;", ">": "&gt;", "&": "&amp;", '"': "&quot;", "'": "&#39;" })[c] ?? c);
}

function speciesLabel(s: PetSpecies) {
  return s === 1 ? "Cachorro" : s === 2 ? "Gato" : "Outro";
}

function sizeLabel(s: PetSize) {
  return s === 1 ? "Pequeno" : s === 2 ? "Medio" : "Grande";
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

type PetFormState = {
  customerProfileId: string;
  name: string;
  species: PetSpecies;
  size: PetSize;
  breed: string;
  weightKg: string;
  birthDate: string;
  behaviorNotes: string;
  allergyNotes: string;
  restrictions: string;
};

function emptyPetForm(): PetFormState {
  return {
    customerProfileId: "",
    name: "",
    species: 1,
    size: 1,
    breed: "",
    weightKg: "",
    birthDate: "",
    behaviorNotes: "",
    allergyNotes: "",
    restrictions: "",
  };
}

export function PetsModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [view, setView] = useState<View>("list");
  const [pets, setPets] = useState<PetDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [filterActive, setFilterActive] = useState<"all" | "true" | "false">("all");
  const [loading, setLoading] = useState(true);
  const [selectedPet, setSelectedPet] = useState<PetDto | null>(null);
  const [form, setForm] = useState<PetFormState>(emptyPetForm());
  const [customers, setCustomers] = useState<CustomerProfileDto[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const [isTogglingStatus, setIsTogglingStatus] = useState(false);
  const [isUploadingPhoto, setIsUploadingPhoto] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const photoInputRef = useRef<HTMLInputElement>(null);
  const pageSize = 25;

  async function loadPets(p = page, s = search, active = filterActive) {
    setLoading(true);
    try {
      const isActive = active === "all" ? undefined : active === "true";
      const res = await getPets(token, { search: s, isActive, page: p, pageSize });
      setPets(res.items);
      setTotal(res.total);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os animais.");
    } finally {
      setLoading(false);
    }
  }

  async function loadCustomers() {
    try {
      const res = await getPetShopCustomers(token, { pageSize: 200 });
      setCustomers(res.items);
    } catch {
      // Non-critical
    }
  }

  useEffect(() => {
    void loadPets();
  }, [token, page, search, filterActive]);

  function handleSearch(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    setSearch(searchInput);
  }

  function openCreate() {
    void loadCustomers();
    setForm(emptyPetForm());
    setErrorMessage("");
    setSuccessMessage("");
    setView("create");
  }

  function openEdit(pet: PetDto) {
    void loadCustomers();
    setForm({
      customerProfileId: pet.customerProfileId,
      name: pet.name,
      species: pet.species,
      size: pet.size,
      breed: pet.breed ?? "",
      weightKg: pet.weightKg !== null && pet.weightKg !== undefined ? String(pet.weightKg) : "",
      birthDate: pet.birthDate ?? "",
      behaviorNotes: pet.behaviorNotes ?? "",
      allergyNotes: pet.allergyNotes ?? "",
      restrictions: pet.restrictions ?? "",
    });
    setSelectedPet(pet);
    setErrorMessage("");
    setSuccessMessage("");
    setView("edit");
  }

  function setFormField<K extends keyof PetFormState>(key: K, value: PetFormState[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setIsSaving(true);
    setErrorMessage("");
    try {
      await createPet(token, {
        customerProfileId: form.customerProfileId,
        name: form.name.trim(),
        species: form.species,
        size: form.size,
        breed: form.breed.trim() || null,
        weightKg: form.weightKg ? Number(form.weightKg) : null,
        birthDate: form.birthDate || null,
        behaviorNotes: form.behaviorNotes.trim() || null,
        allergyNotes: form.allergyNotes.trim() || null,
        restrictions: form.restrictions.trim() || null,
      });
      setSuccessMessage("Animal cadastrado.");
      setView("list");
      setPage(1);
      await loadPets(1, search, filterActive);
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel cadastrar o animal.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleEdit(e: FormEvent) {
    e.preventDefault();
    if (!selectedPet) return;
    setIsSaving(true);
    setErrorMessage("");
    try {
      const updated = await updatePet(token, selectedPet.id, {
        name: form.name.trim(),
        species: form.species,
        size: form.size,
        breed: form.breed.trim() || null,
        weightKg: form.weightKg ? Number(form.weightKg) : null,
        birthDate: form.birthDate || null,
        behaviorNotes: form.behaviorNotes.trim() || null,
        allergyNotes: form.allergyNotes.trim() || null,
        restrictions: form.restrictions.trim() || null,
      });
      setSelectedPet(updated);
      setSuccessMessage("Animal atualizado.");
      setView("detail");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o animal.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleToggleStatus(pet: PetDto) {
    if (!window.confirm(pet.isActive ? "Desativar este animal?" : "Reativar este animal?")) return;
    setIsTogglingStatus(true);
    setErrorMessage("");
    try {
      const updated = await updatePetStatus(token, pet.id, !pet.isActive);
      if (selectedPet?.id === pet.id) setSelectedPet(updated);
      setPets((prev) => prev.map((p) => (p.id === updated.id ? updated : p)));
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel alterar o status.");
    } finally {
      setIsTogglingStatus(false);
    }
  }

  async function handlePhotoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    if (!selectedPet || !e.target.files?.[0]) return;
    const file = e.target.files[0];
    if (file.size > 5 * 1024 * 1024) {
      setErrorMessage("A foto deve ter no maximo 5 MB.");
      return;
    }
    setIsUploadingPhoto(true);
    setErrorMessage("");
    try {
      const res = await uploadPetPhoto(token, selectedPet.id, file);
      setSelectedPet((prev) => (prev ? { ...prev, photoUrl: res.photoUrl } : null));
      setSuccessMessage("Foto atualizada.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel enviar a foto.");
    } finally {
      setIsUploadingPhoto(false);
      if (photoInputRef.current) photoInputRef.current.value = "";
    }
  }

  async function handleDeletePhoto() {
    if (!selectedPet || !window.confirm("Remover a foto?")) return;
    setIsUploadingPhoto(true);
    setErrorMessage("");
    try {
      await deletePetPhoto(token, selectedPet.id);
      setSelectedPet((prev) => (prev ? { ...prev, photoUrl: null } : null));
      setSuccessMessage("Foto removida.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel remover a foto.");
    } finally {
      setIsUploadingPhoto(false);
    }
  }

  const totalPages = Math.ceil(total / pageSize);

  if (view === "create" || view === "edit") {
    const isEdit = view === "edit";
    return (
      <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <h1>{isEdit ? "Editar animal" : "Novo animal"}</h1>
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
              <label htmlFor="ps-p-customer">Tutor</label>
              <select
                id="ps-p-customer"
                required
                value={form.customerProfileId}
                onChange={(e) => setFormField("customerProfileId", e.target.value)}
              >
                <option value="">Selecione o tutor...</option>
                {customers.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name} ({c.phoneNumber})
                  </option>
                ))}
              </select>
            </div>
          )}
          <div className="form-field">
            <label htmlFor="ps-p-name">Nome do animal</label>
            <input
              id="ps-p-name"
              type="text"
              required
              value={form.name}
              onChange={(e) => setFormField("name", e.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="ps-p-species">Especie</label>
            <select
              id="ps-p-species"
              value={form.species}
              onChange={(e) => setFormField("species", Number(e.target.value) as PetSpecies)}
            >
              {SPECIES_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="ps-p-size">Porte</label>
            <select
              id="ps-p-size"
              value={form.size}
              onChange={(e) => setFormField("size", Number(e.target.value) as PetSize)}
            >
              {SIZE_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="ps-p-breed">Raca</label>
            <input
              id="ps-p-breed"
              type="text"
              value={form.breed}
              onChange={(e) => setFormField("breed", e.target.value)}
              placeholder="SRD"
            />
          </div>
          <div className="form-field">
            <label htmlFor="ps-p-weight">Peso (kg)</label>
            <input
              id="ps-p-weight"
              type="number"
              step="0.1"
              min="0"
              value={form.weightKg}
              onChange={(e) => setFormField("weightKg", e.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="ps-p-birth">Data de nascimento</label>
            <input
              id="ps-p-birth"
              type="date"
              value={form.birthDate}
              onChange={(e) => setFormField("birthDate", e.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="ps-p-behavior">Comportamento</label>
            <textarea
              id="ps-p-behavior"
              rows={2}
              value={form.behaviorNotes}
              onChange={(e) => setFormField("behaviorNotes", e.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="ps-p-allergy">Alergias</label>
            <textarea
              id="ps-p-allergy"
              rows={2}
              value={form.allergyNotes}
              onChange={(e) => setFormField("allergyNotes", e.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="ps-p-restrictions">Restricoes</label>
            <textarea
              id="ps-p-restrictions"
              rows={2}
              value={form.restrictions}
              onChange={(e) => setFormField("restrictions", e.target.value)}
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

  if (view === "detail" && selectedPet) {
    return (
      <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <h1>{escapeText(selectedPet.name)}</h1>
            <p className="body-copy">
              {speciesLabel(selectedPet.species)} &middot; {sizeLabel(selectedPet.size)}
              {!selectedPet.isActive && " · Inativo"}
            </p>
          </div>
          <div className="toolbar-actions compact">
            <button type="button" className="secondary-button" onClick={() => setView("list")}>
              Voltar
            </button>
            <button type="button" className="secondary-button" onClick={() => openEdit(selectedPet)}>
              Editar
            </button>
            <button
              type="button"
              className="secondary-button"
              disabled={isTogglingStatus}
              onClick={() => void handleToggleStatus(selectedPet)}
            >
              {selectedPet.isActive ? "Desativar" : "Reativar"}
            </button>
          </div>
        </div>

        {errorMessage && <p className="error-message">{errorMessage}</p>}
        {successMessage && <p className="success-message">{successMessage}</p>}

        <div className="settings-form">
          <p>
            <strong>Tutor:</strong> {escapeText(selectedPet.customerName)}
          </p>
          {selectedPet.breed && (
            <p>
              <strong>Raca:</strong> {escapeText(selectedPet.breed)}
            </p>
          )}
          {selectedPet.weightKg !== null && selectedPet.weightKg !== undefined && (
            <p>
              <strong>Peso:</strong> {selectedPet.weightKg} kg
            </p>
          )}
          {selectedPet.birthDate && (
            <p>
              <strong>Nascimento:</strong> {selectedPet.birthDate}
            </p>
          )}
          {selectedPet.allergyNotes && (
            <p>
              <strong>Alergias:</strong> {escapeText(selectedPet.allergyNotes)}
            </p>
          )}
          {selectedPet.restrictions && (
            <p>
              <strong>Restricoes:</strong> {escapeText(selectedPet.restrictions)}
            </p>
          )}
          {selectedPet.behaviorNotes && (
            <p>
              <strong>Comportamento:</strong> {escapeText(selectedPet.behaviorNotes)}
            </p>
          )}
        </div>

        <div style={{ marginTop: "1.5rem" }}>
          <h2>Foto</h2>
          {selectedPet.photoUrl && (
            <div style={{ marginBottom: "0.75rem" }}>
              <img
                src={selectedPet.photoUrl}
                alt={`Foto de ${selectedPet.name}`}
                style={{ maxWidth: 200, maxHeight: 200, borderRadius: 8, objectFit: "cover" }}
              />
            </div>
          )}
          <div className="toolbar-actions compact">
            <button
              type="button"
              className="secondary-button"
              disabled={isUploadingPhoto}
              onClick={() => photoInputRef.current?.click()}
            >
              {isUploadingPhoto ? "Enviando..." : selectedPet.photoUrl ? "Trocar foto" : "Adicionar foto"}
            </button>
            {selectedPet.photoUrl && (
              <button
                type="button"
                className="secondary-button"
                disabled={isUploadingPhoto}
                onClick={() => void handleDeletePhoto()}
              >
                Remover foto
              </button>
            )}
          </div>
          <input
            ref={photoInputRef}
            type="file"
            accept="image/*"
            style={{ display: "none" }}
            onChange={(e) => void handlePhotoUpload(e)}
          />
          <p className="body-copy" style={{ marginTop: "0.5rem", fontSize: "0.875em" }}>
            Maximo 5 MB.
          </p>
        </div>
      </section>
    );
  }

  return (
    <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
      <div className="workspace-summary-head">
        <div className="hero-stack">
          <h1>Animais</h1>
          {total > 0 && <p className="body-copy">{total} animal(is) encontrado(s)</p>}
        </div>
        <div className="toolbar-actions compact">
          <button type="button" className="primary-button" onClick={openCreate}>
            Novo animal
          </button>
        </div>
      </div>

      {errorMessage && <p className="error-message">{errorMessage}</p>}
      {successMessage && <p className="success-message">{successMessage}</p>}

      <div style={{ display: "flex", gap: "0.75rem", marginBottom: "1rem", flexWrap: "wrap" }}>
        <form onSubmit={handleSearch} style={{ display: "flex", gap: "0.5rem", flex: 1 }}>
          <input
            type="search"
            placeholder="Buscar por nome..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            style={{ flex: 1 }}
          />
          <button type="submit" className="secondary-button">
            Buscar
          </button>
        </form>
        <select
          value={filterActive}
          onChange={(e) => {
            setFilterActive(e.target.value as "all" | "true" | "false");
            setPage(1);
          }}
          className="secondary-button"
        >
          <option value="all">Todos</option>
          <option value="true">Ativos</option>
          <option value="false">Inativos</option>
        </select>
      </div>

      {loading && <p className="workspace-inline-loading">Carregando...</p>}

      {!loading && pets.length === 0 && (
        <p className="body-copy">Nenhum animal encontrado{search ? " para essa busca" : ""}.</p>
      )}

      {!loading && pets.length > 0 && (
        <ul className="simple-list">
          {pets.map((pet) => (
            <li
              key={pet.id}
              className="simple-list-item simple-list-item--clickable"
              onClick={() => {
                setSelectedPet(pet);
                setSuccessMessage("");
                setErrorMessage("");
                setView("detail");
              }}
            >
              <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
                {pet.photoUrl && (
                  <img
                    src={pet.photoUrl}
                    alt={pet.name}
                    style={{ width: 40, height: 40, borderRadius: 4, objectFit: "cover" }}
                  />
                )}
                <div>
                  <strong>
                    {escapeText(pet.name)}
                    {!pet.isActive && <span style={{ color: "var(--color-muted)", marginLeft: 6 }}>(Inativo)</span>}
                  </strong>
                  <br />
                  <span className="body-copy">
                    {speciesLabel(pet.species)} &middot; {sizeLabel(pet.size)} &middot;{" "}
                    {escapeText(pet.customerName)}
                  </span>
                </div>
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
