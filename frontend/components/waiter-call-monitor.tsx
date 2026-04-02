"use client";

import { useEffect, useRef, useState } from "react";
import { getCompanySettings, getWaiterCalls, getWorkspaceAlertsSignal, resolveWaiterCall, type AlertSettings, type WaiterCall } from "@/lib/api";
import { formatDateTime, handleApiError } from "@/components/modules/module-utils";
import { useAppSession } from "@/components/app-session-provider";

const DEFAULT_ALERT_SOUND_SRC = "/sounds/instrumento.wav?v=20260322-3";
const DEFAULT_ALERT_VOLUME_PERCENT = 100;
const DEFAULT_ALERT_PLAYBACK_SECONDS = 6;

function clampAlertVolume(volumePercent?: number) {
  const normalized = typeof volumePercent === "number" ? volumePercent : DEFAULT_ALERT_VOLUME_PERCENT;
  return Math.min(Math.max(normalized / 100, 0), 1);
}

function buildWaiterAlertAudio(soundUrl?: string | null, volumePercent?: number) {
  const audio = new Audio(soundUrl || DEFAULT_ALERT_SOUND_SRC);
  audio.preload = "auto";
  audio.volume = clampAlertVolume(volumePercent);
  return audio;
}

function playFallbackAlert(volumePercent?: number) {
  const AudioContextClass = window.AudioContext || (window as typeof window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;

  if (!AudioContextClass) {
    return;
  }

  const context = new AudioContextClass();
  const gain = context.createGain();
  gain.gain.value = clampAlertVolume(volumePercent);
  gain.connect(context.destination);

  const notes = [
    { frequency: 740, start: 0, duration: 0.16, volume: 0.18 },
    { frequency: 988, start: 0.22, duration: 0.16, volume: 0.2 },
    { frequency: 1318, start: 0.44, duration: 0.24, volume: 0.24 },
    { frequency: 988, start: 0.82, duration: 0.16, volume: 0.18 },
    { frequency: 1480, start: 1.02, duration: 0.28, volume: 0.28 },
  ];

  notes.forEach((note) => {
    const oscillator = context.createOscillator();
    oscillator.type = "triangle";
    oscillator.frequency.setValueAtTime(note.frequency, context.currentTime + note.start);

    const oscillatorGain = context.createGain();
    oscillatorGain.gain.setValueAtTime(0.0001, context.currentTime + note.start);
    oscillatorGain.gain.exponentialRampToValueAtTime(note.volume, context.currentTime + note.start + 0.02);
    oscillatorGain.gain.exponentialRampToValueAtTime(0.0001, context.currentTime + note.start + note.duration);

    oscillator.connect(oscillatorGain);
    oscillatorGain.connect(gain);
    oscillator.start(context.currentTime + note.start);
    oscillator.stop(context.currentTime + note.start + note.duration);
  });

  window.setTimeout(() => {
    void context.close();
  }, 1700);
}

function getLatestWaiterCall(waiterCalls: WaiterCall[]) {
  return waiterCalls.reduce<WaiterCall | null>((latestValue, waiterCall) => {
    if (!latestValue || waiterCall.requestedAtUtc > latestValue.requestedAtUtc) {
      return waiterCall;
    }

    return latestValue;
  }, null);
}

function isNewSignal(nextSignal: string, currentSignal: string) {
  return Boolean(nextSignal) && (!currentSignal || nextSignal > currentSignal);
}

function summarizeWaiterCalls(waiterCalls: WaiterCall[]) {
  const tableNames = [...new Set(waiterCalls.map((waiterCall) => waiterCall.tableName))];

  if (tableNames.length === 0) {
    return "";
  }

  if (tableNames.length === 1) {
    return `Mesa chamando: ${tableNames[0]}.`;
  }

  if (tableNames.length === 2) {
    return `Mesas chamando: ${tableNames[0]} e ${tableNames[1]}.`;
  }

  return `Mesas chamando: ${tableNames.slice(0, 3).join(", ")}${tableNames.length > 3 ? "..." : ""}.`;
}

function stopCurrentAlert(
  audioRef: React.MutableRefObject<HTMLAudioElement | null>,
  stopTimeoutRef: React.MutableRefObject<number | null>,
) {
  if (stopTimeoutRef.current) {
    window.clearTimeout(stopTimeoutRef.current);
    stopTimeoutRef.current = null;
  }

  if (audioRef.current) {
    audioRef.current.pause();
    audioRef.current.currentTime = 0;
    audioRef.current.loop = false;
  }
}

async function playWaiterAlertSound(
  audioRef: React.MutableRefObject<HTMLAudioElement | null>,
  stopTimeoutRef: React.MutableRefObject<number | null>,
  alertSettings: AlertSettings,
) {
  try {
    if (!audioRef.current) {
      audioRef.current = buildWaiterAlertAudio(alertSettings.soundUrl, alertSettings.volumePercent);
    }

    stopCurrentAlert(audioRef, stopTimeoutRef);

    audioRef.current.volume = clampAlertVolume(alertSettings.volumePercent);
    audioRef.current.loop = true;
    await audioRef.current.play();

    const playbackSeconds = Math.max(alertSettings.playbackSeconds || DEFAULT_ALERT_PLAYBACK_SECONDS, 1);
    stopTimeoutRef.current = window.setTimeout(() => {
      stopCurrentAlert(audioRef, stopTimeoutRef);
    }, playbackSeconds * 1000);

    return true;
  } catch {
    try {
      playFallbackAlert(alertSettings.volumePercent);
      return true;
    } catch {
      return false;
    }
  }
}

export function WaiterCallMonitor({ showCard = true }: { showCard?: boolean }) {
  const { session, clearSession } = useAppSession();
  const [waiterCalls, setWaiterCalls] = useState<WaiterCall[]>([]);
  const [alertSettings, setAlertSettings] = useState<AlertSettings>({
    enableOrderAlerts: true,
    enableWaiterCallAlerts: true,
    soundUrl: DEFAULT_ALERT_SOUND_SRC,
    hasCustomSound: false,
    volumePercent: DEFAULT_ALERT_VOLUME_PERCENT,
    playbackSeconds: DEFAULT_ALERT_PLAYBACK_SECONDS,
  });
  const [processingId, setProcessingId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [soundMessage, setSoundMessage] = useState("");
  const latestWaiterSignalRef = useRef("");
  const latestOrderSignalRef = useRef("");
  const initializedRef = useRef(false);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const stopTimeoutRef = useRef<number | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function loadAlertSettings() {
      try {
        const response = await getCompanySettings(session.token);

        if (!isMounted) {
          return;
        }

        setAlertSettings(response.alerts);
      } catch {
        if (!isMounted) {
          return;
        }

        setAlertSettings((currentValue) => currentValue);
      }
    }

    void loadAlertSettings();

    return () => {
      isMounted = false;
    };
  }, [session.token]);

  useEffect(() => {
    let isMounted = true;

    async function loadSignals() {
      const [signalsResult, waiterCallsResult] = await Promise.allSettled([
        getWorkspaceAlertsSignal(session.token),
        showCard ? getWaiterCalls(session.token) : Promise.resolve<WaiterCall[]>([]),
      ]);

      if (!isMounted) {
        return;
      }

      const nextSignals = signalsResult.status === "fulfilled" ? signalsResult.value : null;
      const nextWaiterCalls = waiterCallsResult.status === "fulfilled" ? waiterCallsResult.value : waiterCalls;

      if (showCard && waiterCallsResult.status === "fulfilled") {
        setWaiterCalls(waiterCallsResult.value);
      }

      if (signalsResult.status === "rejected" && (!showCard || waiterCallsResult.status === "rejected")) {
        await handleApiError(
          signalsResult.reason,
          clearSession,
          setErrorMessage,
          "Nao foi possivel carregar os alertas da operacao.",
        );
        return;
      }

      setErrorMessage("");

      const nextWaiterSignal = nextSignals?.latestWaiterCallAtUtc ?? "";
      const nextOrderSignal = nextSignals?.latestOrderAtUtc ?? "";

      if (!initializedRef.current) {
        initializedRef.current = true;
        latestWaiterSignalRef.current = nextWaiterSignal;
        latestOrderSignalRef.current = nextOrderSignal;
        return;
      }

      const hasNewWaiterCall = signalsResult.status === "fulfilled" && isNewSignal(nextWaiterSignal, latestWaiterSignalRef.current);
      const hasNewOrder = signalsResult.status === "fulfilled" && isNewSignal(nextOrderSignal, latestOrderSignalRef.current);

      if (signalsResult.status === "fulfilled") {
        latestWaiterSignalRef.current = nextWaiterSignal;
        latestOrderSignalRef.current = nextOrderSignal;
      }

      const shouldPlayWaiterAlert = hasNewWaiterCall && alertSettings.enableWaiterCallAlerts;
      const shouldPlayOrderAlert = hasNewOrder && alertSettings.enableOrderAlerts;

      if (!shouldPlayWaiterAlert && !shouldPlayOrderAlert) {
        return;
      }

      const latestWaiterCall = shouldPlayWaiterAlert
        ? (showCard ? getLatestWaiterCall(nextWaiterCalls) : null)
        : null;
      const latestTableSoundUrl = latestWaiterCall?.tableAlertSoundUrl ?? nextSignals?.latestWaiterCallTableSoundUrl;
      const playbackSettings = shouldPlayWaiterAlert && latestTableSoundUrl
        ? { ...alertSettings, soundUrl: latestTableSoundUrl }
        : alertSettings;

      const didPlay = await playWaiterAlertSound(audioRef, stopTimeoutRef, playbackSettings);

      if (!isMounted) {
        return;
      }

      if (!didPlay) {
        setSoundMessage("Clique em Testar som uma vez para liberar o audio neste navegador.");
        return;
      }
      setSoundMessage("");
    }

    void loadSignals();
    const intervalId = window.setInterval(() => {
      void loadSignals();
    }, showCard ? 8000 : 12000);

    return () => {
      isMounted = false;
      window.clearInterval(intervalId);
    };
  }, [
    alertSettings.enableOrderAlerts,
    alertSettings.enableWaiterCallAlerts,
    alertSettings.playbackSeconds,
    alertSettings.soundUrl,
    alertSettings.volumePercent,
    session.token,
    showCard,
  ]);

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    stopCurrentAlert(audioRef, stopTimeoutRef);
    audioRef.current = buildWaiterAlertAudio(alertSettings.soundUrl, alertSettings.volumePercent);
    audioRef.current.load();

    return () => {
      stopCurrentAlert(audioRef, stopTimeoutRef);
      audioRef.current = null;
    };
  }, [alertSettings.soundUrl, alertSettings.volumePercent]);

  async function handleResolve(waiterCallId: string) {
    try {
      setProcessingId(waiterCallId);
      await resolveWaiterCall(session.token, waiterCallId);
      setWaiterCalls((currentValue) => {
        const nextCalls = currentValue.filter((item) => item.id !== waiterCallId);

        if (nextCalls.length === 0) {
          setSoundMessage("");
        }

        return nextCalls;
      });
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, clearSession, setErrorMessage, "Nao foi possivel marcar o atendimento.");
    } finally {
      setProcessingId("");
    }
  }

  if (!showCard) {
    return null;
  }

  if (!waiterCalls.length && !errorMessage && !soundMessage) {
    return null;
  }

  async function handleTestSound() {
    setSoundMessage("");

    const didPlay = await playWaiterAlertSound(audioRef, stopTimeoutRef, alertSettings);

    if (!didPlay) {
      setSoundMessage("Nao foi possivel tocar o som agora.");
      return;
    }

    setSoundMessage("Som testado com sucesso.");
  }

  return (
    <section className={`surface-card waiter-call-board ${waiterCalls.length ? "waiter-call-board-active" : ""}`}>
      <div className="module-section-head waiter-call-head">
        <div>
          <span className="eyebrow">Operacao</span>
          <h2>Alertas sonoros</h2>
        </div>
        <div className="waiter-call-tools">
          <strong>{waiterCalls.length} chamadas</strong>
          <button className="ghost-link button-link waiter-call-test-button" type="button" onClick={() => void handleTestSound()}>
            Testar som
          </button>
        </div>
      </div>

      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      {soundMessage ? <p className="waiter-call-notice">{soundMessage}</p> : null}
      {waiterCalls.length ? <p className="waiter-call-summary">{summarizeWaiterCalls(waiterCalls)}</p> : null}

      {waiterCalls.length ? (
        <div className="waiter-call-list">
          {waiterCalls.map((waiterCall) => (
            <article key={waiterCall.id} className="waiter-call-card">
              <div>
                <strong>{waiterCall.tableName}</strong>
                <p>{formatDateTime(waiterCall.requestedAtUtc)}</p>
              </div>

              <button
                className="primary-link button-link waiter-call-action"
                type="button"
                disabled={processingId === waiterCall.id}
                onClick={() => void handleResolve(waiterCall.id)}
              >
                {processingId === waiterCall.id ? "Atendendo..." : "Marcar atendido"}
              </button>
            </article>
          ))}
        </div>
      ) : (
        <div className="module-empty-state compact-empty-state">
          <p>Nenhuma chamada pendente.</p>
        </div>
      )}
    </section>
  );
}
