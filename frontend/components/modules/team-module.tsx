"use client";

import { FormEvent, useEffect, useState } from "react";
import { createTeamMember, getTeamMembers, type TeamMember } from "@/lib/api";
import {
  emptyTeamDraft,
  formatDateTime,
  handleApiError,
  type AsyncVoid,
  type TeamDraft,
} from "@/components/modules/module-utils";

export function TeamModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [members, setMembers] = useState<TeamMember[]>([]);
  const [draft, setDraft] = useState<TeamDraft>(emptyTeamDraft());
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadTeam() {
    setLoading(true);

    try {
      setMembers(await getTeamMembers(token));
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar a equipe.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadTeam();
  }, [token]);

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSuccessMessage("");

    try {
      await createTeamMember(token, draft);
      setDraft(emptyTeamDraft());
      setSuccessMessage("Acesso criado para a equipe.");
      await loadTeam();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar o acesso.");
    }
  }

  return (
    <section className="module-body-grid">
      <section className="surface-card module-form-card">
        <span className="eyebrow">Novo acesso</span>
        <h2>Adicionar membro da equipe</h2>
        <form className="module-form" onSubmit={handleCreate}>
          <div className="field-group">
            <label>Nome completo</label>
            <input value={draft.fullName} onChange={(event) => setDraft((current) => ({ ...current, fullName: event.target.value }))} />
          </div>

          <div className="module-inline-grid">
            <div className="field-group">
              <label>Email</label>
              <input value={draft.email} onChange={(event) => setDraft((current) => ({ ...current, email: event.target.value }))} />
            </div>
            <div className="field-group">
              <label>Senha</label>
              <input
                type="password"
                value={draft.password}
                onChange={(event) => setDraft((current) => ({ ...current, password: event.target.value }))}
              />
            </div>
          </div>

          <div className="field-group">
            <label>Perfil</label>
            <select value={draft.role} onChange={(event) => setDraft((current) => ({ ...current, role: event.target.value }))}>
              <option value="Owner">Owner</option>
              <option value="Manager">Manager</option>
              <option value="Employee">Employee</option>
            </select>
          </div>

          <button className="primary-link button-link" type="submit">
            Criar acesso
          </button>
        </form>

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <span className="eyebrow">Equipe</span>
          <strong>{members.length} acessos ativos</strong>
        </div>

        {loading ? (
          <p className="loading-state">Carregando equipe...</p>
        ) : members.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhum acesso criado.</strong>
            <p>Adicione gerencia e equipe para separar a rotina da unidade por perfil.</p>
          </div>
        ) : (
          <div className="module-card-list">
            {members.map((member) => (
              <article key={member.id} className="module-entity-card interactive-card">
                <div className="entity-head">
                  <div>
                    <h3>{member.fullName}</h3>
                    <p>{member.email}</p>
                  </div>
                  <span className={`status-chip ${member.role.toLowerCase()}`}>{member.role}</span>
                </div>

                <div className="entity-meta-grid">
                  <span>{member.isActive ? "Ativo" : "Inativo"}</span>
                  <span>{member.lastLoginAtUtc ? formatDateTime(member.lastLoginAtUtc) : "Sem login"}</span>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
