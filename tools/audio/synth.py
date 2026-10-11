"""BAIT & SWITCH sound kit: every music loop and sound effect, synthesized from scratch (no samples, no AI audio).

  python -I tools/audio/synth.py [out_dir]      (default: Assets/Resources/Audio)

Pure Python (no numpy), mono 16-bit WAV. Loops are made seamless by folding their tails back into the start.
Day: a slightly detuned music box over low strings (3/4, D minor). Dusk = the Day clip played at pitch 0.944
(slower and a semitone flat, done in Unity). Night: drone, a formant "aah" choir and sparse bells.
"""
import math
import os
import random
import sys
import wave
from array import array

OUT = sys.argv[1] if len(sys.argv) > 1 else "Assets/Resources/Audio"
TAU = math.tau
rnd = random.Random(7)


# ----------------------------------------------------------------------------------------------- basics

def z(n):
    return [0.0] * n


def ns(sec, sr):
    return int(sec * sr)


def add(dst, src, at=0, gain=1.0):
    n = min(len(src), len(dst) - at)
    for i in range(max(0, -at), n):
        dst[at + i] += src[i] * gain


def write(name, x, sr, peak=0.9):
    m = max(1e-9, max(abs(v) for v in x))
    g = peak / m
    data = array("h", (int(max(-1.0, min(1.0, v * g)) * 32767) for v in x))
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(data.tobytes())
    print(f"  {name}.wav  {len(x) / sr:5.2f}s")


def fade(x, a_sec, r_sec, sr):
    a, r = ns(a_sec, sr), ns(r_sec, sr)
    for i in range(min(a, len(x))):
        x[i] *= i / max(1, a)
    for i in range(min(r, len(x))):
        x[len(x) - 1 - i] *= i / max(1, r)
    return x


def fold_loop(x, length):
    """Fold everything past `length` back onto the start: a click-free loop of exactly `length` samples."""
    out = x[:length]
    for i in range(length, len(x)):
        out[(i - length) % length] += x[i]
    return out


def midi(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


# ----------------------------------------------------------------------------------------------- filters

def lowpass(x, fc, sr, passes=1):
    a = 1 - math.exp(-TAU * fc / sr)
    for _ in range(passes):
        y = 0.0
        for i in range(len(x)):
            y += a * (x[i] - y)
            x[i] = y
    return x


def highpass(x, fc, sr):
    a = 1 - math.exp(-TAU * fc / sr)
    y = 0.0
    for i in range(len(x)):
        y += a * (x[i] - y)
        x[i] = x[i] - y
    return x


def bandpass(x, fc, q, sr):
    w0 = TAU * fc / sr
    al = math.sin(w0) / (2 * q)
    b0, b2 = al, -al
    a0, a1, a2 = 1 + al, -2 * math.cos(w0), 1 - al
    b0, b2, a1, a2 = b0 / a0, b2 / a0, a1 / a0, a2 / a0
    x1 = x2 = y1 = y2 = 0.0
    out = z(len(x))
    for i in range(len(x)):
        xi = x[i]
        y = b0 * xi + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1, y2, y1 = x1, xi, y1, y
        out[i] = y
    return out


def sweep_lowpass(x, f0, f1, sr):
    n = len(x)
    y = 0.0
    for i in range(n):
        fc = f0 * (f1 / f0) ** (i / max(1, n - 1))
        a = 1 - math.exp(-TAU * fc / sr)
        y += a * (x[i] - y)
        x[i] = y
    return x


def reverb(x, sr, size=1.0, fb=0.84, damp=0.3, tail=2.5):
    """Schroeder/Freeverb-ish mono reverb. Returns the wet signal (with tail appended)."""
    n = len(x) + ns(tail, sr)
    src = x + z(n - len(x))
    out = z(n)
    k = sr / 44100.0 * size
    for d in (1557, 1617, 1491, 1422, 1277, 1356):
        dl = max(1, int(d * k))
        buf = z(dl)
        idx = 0
        lp = 0.0
        for i in range(n):
            o = buf[idx]
            lp = o * (1 - damp) + lp * damp
            buf[idx] = src[i] + lp * fb
            idx += 1
            if idx == dl:
                idx = 0
            out[i] += o
    for d in (556, 441, 341):
        dl = max(1, int(d * k))
        buf = z(dl)
        idx = 0
        for i in range(n):
            b = buf[idx]
            v = out[i]
            o = -v + b
            buf[idx] = v + b * 0.5
            idx += 1
            if idx == dl:
                idx = 0
            out[i] = o
    return [v * 0.12 for v in out]


def mix_reverb(x, sr, wet=0.3, **kw):
    w = reverb(x, sr, **kw)
    out = x + z(len(w) - len(x))
    for i in range(len(w)):
        out[i] += w[i] * wet
    return out


# ----------------------------------------------------------------------------------------------- voices

def music_box(f, sr, dur=1.8, vel=1.0, detune_cents=0.0):
    """Struck comb tooth: fundamental plus inharmonic partials that die fast, a tiny click, slow warble."""
    f *= 2 ** (detune_cents / 1200)
    n = ns(dur, sr)
    out = z(n)
    parts = ((1.0, 1.0, 1.5), (2.76, 0.35, 5.0), (5.4, 0.16, 9.0), (8.93, 0.07, 14.0))
    for ratio, amp, decay in parts:
        fr = f * ratio
        if fr > sr * 0.45:
            continue
        ph = rnd.random() * TAU
        inc = TAU * fr / sr
        for i in range(n):
            t = i / sr
            out[i] += math.sin(ph + inc * i + 0.002 * math.sin(TAU * 0.35 * t)) * amp * math.exp(-t * decay)
    for i in range(min(n, ns(0.004, sr))):
        out[i] += (rnd.random() * 2 - 1) * 0.25 * (1 - i / ns(0.004, sr))
    return [v * vel for v in out]


def saw_pad(freqs, sr, dur, attack=0.6, release=0.9, detune=0.006, vib=0.004):
    n = ns(dur + release, sr)
    out = z(n)
    for f in freqs:
        for d in (-detune, detune):
            fr = f * (1 + d)
            ph = rnd.random()
            for i in range(n):
                t = i / sr
                ph += fr * (1 + vib * math.sin(TAU * 4.8 * t)) / sr
                ph -= int(ph)
                out[i] += 2 * ph - 1
    a, sus = ns(attack, sr), ns(dur, sr)
    for i in range(n):
        e = min(1.0, i / max(1, a))
        if i > sus:
            e *= max(0.0, 1 - (i - sus) / max(1, ns(release, sr)))
        out[i] *= e / (2 * len(freqs))
    return out


def sine_note(f, sr, dur, attack=0.02, release=0.3, amp=1.0):
    n = ns(dur + release, sr)
    out = z(n)
    inc = TAU * f / sr
    a, sus, r = ns(attack, sr), ns(dur, sr), ns(release, sr)
    for i in range(n):
        e = min(1.0, i / max(1, a))
        if i > sus:
            e *= max(0.0, 1 - (i - sus) / max(1, r))
        out[i] = math.sin(inc * i) * e * amp
    return out


def bell(f, sr, dur=3.0, index=3.0, ratio=1.4, amp=1.0):
    """FM bell (inharmonic ratio): church-toll for round starts, chimes."""
    n = ns(dur, sr)
    out = z(n)
    for i in range(n):
        t = i / sr
        env = math.exp(-t * 2.2 / dur * 3)
        out[i] = math.sin(TAU * f * t + index * env * math.sin(TAU * f * ratio * t)) * env * amp
    return out


def pluck(f, sr, dur, bright=0.5, bend=0.0):
    """Karplus-Strong string: line twangs, snaps."""
    n = ns(dur, sr)
    period = max(2, int(sr / f))
    buf = [rnd.random() * 2 - 1 for _ in range(period)]
    out = z(n)
    idx = 0
    for i in range(n):
        nxt = (idx + 1) % period
        v = buf[idx]
        buf[idx] = (v * (0.5 + bright * 0.5) + buf[nxt] * (0.5 - bright * 0.5)) * 0.996
        out[i] = v
        idx = nxt
        if bend and i % 64 == 0 and period > 4:
            pass
    return out


def noise(n, color="white"):
    out = z(n)
    b = 0.0
    for i in range(n):
        w = rnd.random() * 2 - 1
        if color == "brown":
            b = (b + 0.02 * w) * 0.998
            out[i] = b * 6
        else:
            out[i] = w
    return out


def env_exp(x, sr, attack, decay):
    a = ns(attack, sr)
    for i in range(len(x)):
        t = i / sr
        x[i] *= (i / a if i < a else 1.0) * math.exp(-max(0.0, t - attack) / decay)
    return x


def bubble(sr, f0=None, tau=None, rise=None, amp=1.0):
    f0 = f0 or rnd.uniform(380, 950)
    tau = tau or rnd.uniform(0.018, 0.045)
    rise = rise or rnd.uniform(8, 22)
    n = ns(tau * 6, sr)
    out = z(n)
    ph = 0.0
    for i in range(n):
        t = i / sr
        ph += TAU * f0 * (1 + rise * t) / sr
        out[i] = math.sin(ph) * math.exp(-t / tau) * amp * min(1.0, i / (sr * 0.002))
    return out


def sweep_sine(f0, f1, sr, dur, decay=None, amp=1.0):
    n = ns(dur, sr)
    out = z(n)
    ph = 0.0
    for i in range(n):
        k = i / max(1, n - 1)
        ph += TAU * (f0 * (f1 / f0) ** k) / sr
        e = math.exp(-i / sr / decay) if decay else (1 - k)
        out[i] = math.sin(ph) * e * amp * min(1.0, i / (sr * 0.003))
    return out


def choir(chord, sr, dur, attack=1.2, release=1.5, vowel=(800, 1150, 2900)):
    """Formant 'aah' choir: detuned saws with vibrato through three vocal formants."""
    n = ns(dur + release, sr)
    src = z(n)
    for f in chord:
        for d in (-0.004, 0.0, 0.005):
            fr = f * (1 + d)
            ph = rnd.random()
            vr = rnd.uniform(4.5, 5.5)
            for i in range(n):
                ph += fr * (1 + 0.006 * math.sin(TAU * vr * i / sr)) / sr
                ph -= int(ph)
                src[i] += 2 * ph - 1
    out = z(n)
    for fc, q, g in ((vowel[0], 6, 1.0), (vowel[1], 8, 0.6), (vowel[2], 10, 0.25)):
        add(out, bandpass(src, fc, q, sr), 0, g)
    a, sus, r = ns(attack, sr), ns(dur, sr), ns(release, sr)
    for i in range(n):
        e = min(1.0, i / max(1, a))
        if i > sus:
            e *= max(0.0, 1 - (i - sus) / max(1, r))
        out[i] *= e
    return out


# ----------------------------------------------------------------------------------------------- music

def music_day():
    sr = 32000
    bpm = 76.0
    beat = 60.0 / bpm
    bars = 16
    length = ns(bars * 3 * beat, sr)
    x = z(length + ns(4, sr))
    # chords per bar (D minor): Dm Dm Bb Bb Gm Gm A A Dm Dm F F Gm A Dm Dm
    D, F, A, Bb, G, C, Cs, E = 50, 53, 57, 46, 55, 48, 49, 52
    chords = [(D, F, A), (D, F, A), (Bb, D + 12 - 12, F), (Bb, D, F), (G, Bb + 12, D + 12), (G, Bb + 12, D + 12),
              (A - 12, Cs + 12, E), (A - 12, Cs + 12, E), (D, F, A), (D, F, A), (F, A, C + 12), (F, A, C + 12),
              (G, Bb + 12, D + 12), (A - 12, Cs + 12, E), (D, F, A), (D, F, A)]
    strings = z(len(x))
    bass = z(len(x))
    for b, ch in enumerate(chords):
        at = ns(b * 3 * beat, sr)
        add(strings, saw_pad([midi(m) for m in ch], sr, 3 * beat, attack=0.5, release=0.9), at)
        add(bass, sine_note(midi(ch[0] - 12), sr, 3 * beat * 0.95, attack=0.08, release=0.5, amp=0.5), at)
    lowpass(strings, 850, sr, passes=2)
    # melody: (bar, beat, midi, velocity)
    mel = [
        (0, 0, 74, 1), (0, 1, 77, .8), (0, 2, 81, .9),
        (1, 0, 79, 1), (1, 1, 77, .8), (1, 2, 76, .8),
        (2, 0, 77, 1), (2, 1, 74, .8), (2, 2, 70, .8),
        (3, 0, 72, .9), (3, 1, 74, 1),
        (4, 0, 79, 1), (4, 1, 82, .85), (4, 2, 86, .9),
        (5, 0, 84, 1), (5, 1, 82, .8), (5, 2, 81, .8),
        (6, 0, 81, 1), (6, 1, 79, .8), (6, 2, 76, .8),
        (7, 0, 73, 1),
        (8, 0, 74, 1), (8, 1, 77, .8), (8, 2, 81, .9),
        (9, 0, 86, 1), (9, 1, 84, .8), (9, 2, 81, .8),
        (10, 0, 84, 1), (10, 1, 81, .8), (10, 2, 77, .8),
        (11, 0, 79, .9), (11, 1, 81, 1),
        (12, 0, 82, 1), (12, 1, 81, .8), (12, 2, 79, .8),
        (13, 0, 77, 1), (13, 1, 76, .8), (13, 2, 73, .8),
        (14, 0, 74, 1),
        (15, 0, 69, .6), (15, 2, 74, .5),
    ]
    box = z(len(x))
    for bar, bt, m, v in mel:
        at = ns((bar * 3 + bt) * beat, sr)
        add(box, music_box(midi(m), sr, dur=2.2, vel=v, detune_cents=rnd.uniform(-9, 9)), at)
        if rnd.random() < 0.18:   # a ghost note an octave up, the comb catching
            add(box, music_box(midi(m + 12), sr, dur=1.2, vel=v * 0.25, detune_cents=rnd.uniform(-12, 12)), at + ns(beat * 0.5, sr))
    mixd = z(len(x))
    add(mixd, box, 0, 0.55)
    add(mixd, strings, 0, 0.42)
    add(mixd, bass, 0, 0.35)
    out = mix_reverb(mixd, sr, wet=0.55, size=1.1, fb=0.86, tail=3.0)
    write("music_day", fold_loop(out, length), sr, peak=0.8)


def music_night():
    sr = 32000
    dur = 64.0
    length = ns(dur, sr)
    x = z(length + ns(5, sr))
    # drone and water pressure
    for i in range(len(x)):
        t = i / sr
        sw = 0.55 + 0.45 * math.sin(TAU * t / 16.0)
        x[i] = (math.sin(TAU * 36.71 * t) * 0.5 + math.sin(TAU * 55.0 * t) * 0.22) * sw
    rumble = lowpass(noise(len(x), "brown"), 140, sr, passes=2)
    add(x, rumble, 0, 0.5)
    # choir: four slow chords, 16 s each
    chords = [(62, 65, 69), (58, 62, 65, 69), (55, 58, 62), (57, 61, 64)]
    for k, ch in enumerate(chords):
        add(x, choir([midi(m) for m in ch], sr, 15.0, attack=4.0, release=4.0), ns(k * 16.0, sr), 0.16)
    # sparse bells, far away
    t = 3.0
    notes = [86, 81, 77, 74, 81, 89, 74]
    j = 0
    while t < dur - 2:
        add(x, music_box(midi(notes[j % len(notes)]), sr, dur=2.5, vel=0.35, detune_cents=rnd.uniform(-20, 20)), ns(t, sr))
        t += rnd.uniform(5.0, 9.0)
        j += 1
    out = mix_reverb(x, sr, wet=0.7, size=1.4, fb=0.89, tail=4.5)
    write("music_night", fold_loop(out, length), sr, peak=0.75)


def tension():
    sr = 44100
    beat = 60.0 / 76.0
    length = ns(4 * 3 * beat, sr)
    x = z(length + ns(1, sr))
    for b in range(12):
        at = ns(b * beat, sr)
        if b % 3 == 0:   # heartbeat: lub-dub
            add(x, sweep_sine(70, 38, sr, 0.28, decay=0.09), at, 1.0)
            add(x, sweep_sine(62, 36, sr, 0.22, decay=0.07), at + ns(0.24, sr), 0.65)
        for e in range(2):  # ticking shaker on eighths, accent off-beats
            tk = highpass(env_exp(noise(ns(0.05, sr)), sr, 0.001, 0.012), 5000, sr)
            add(x, tk, at + ns(e * beat * 0.5, sr), 0.22 if e else 0.12)
        if b % 3 == 2:   # a low, muffled tom
            add(x, sweep_sine(110, 70, sr, 0.3, decay=0.1), at + ns(beat * 0.5, sr), 0.35)
    out = mix_reverb(x, sr, wet=0.2, size=0.6, fb=0.7, tail=0.8)
    write("tension", fold_loop(out, length), sr, peak=0.7)


def ambience_underwater():
    sr = 32000
    length = ns(20.0, sr)
    x = lowpass(noise(length + ns(3, sr), "brown"), 260, sr, passes=2)
    for i in range(len(x)):
        x[i] *= 0.7 + 0.3 * math.sin(TAU * i / sr / 10.0)
    t = 0.5
    while t < 21:
        for _ in range(rnd.randint(1, 4)):
            add(x, bubble(sr, amp=0.12), ns(t + rnd.uniform(0, 0.25), sr))
        t += rnd.uniform(0.8, 2.6)
    x = lowpass(x, 1800, sr)
    write("amb_underwater", fold_loop(x, length), sr, peak=0.6)


def ambience_waves():
    sr = 32000
    length = ns(24.0, sr)
    n = length + ns(4, sr)
    src = noise(n)
    out = z(n)
    y = 0.0
    for i in range(n):
        t = i / sr
        swell = 0.5 + 0.5 * math.sin(TAU * t / 6.0) * math.sin(TAU * t / 8.0 + 1.0)
        fc = 250 + 900 * swell
        a = 1 - math.exp(-TAU * fc / sr)
        y += a * (src[i] - y)
        out[i] = y * (0.25 + 0.75 * swell)
    write("amb_waves", fold_loop(out, length), sr, peak=0.55)


# ----------------------------------------------------------------------------------------------- effects

def sfx():
    sr = 44100

    def W(name, x, peak=0.9):
        write(name, x, sr, peak)

    # UI: soft wooden tick and a deeper confirm
    def tick(f, d):
        x = env_exp(noise(ns(0.05, sr)), sr, 0.0005, d)
        x = bandpass(x, f, 4, sr)
        add(x, sweep_sine(f * 1.2, f, sr, 0.05, decay=0.012), 0, 0.5)
        return x
    W("ui_click", tick(1800, 0.008), 0.7)
    W("ui_hover", tick(2600, 0.004), 0.35)
    conf = bell(220, sr, 1.6, index=2.0, ratio=2.0, amp=0.6)
    add(conf, tick(900, 0.01), 0, 0.8)
    W("ui_confirm", mix_reverb(conf, sr, wet=0.4, tail=1.2), 0.75)

    # bubbles
    for k in range(3):
        b = z(ns(0.3, sr))
        for j in range(2 + k):
            add(b, bubble(sr), ns(rnd.uniform(0, 0.12), sr), 0.8)
        W(f"bubble_{k}", lowpass(b, 3500, sr), 0.6)

    # dash: swish + bubble burst
    d = sweep_lowpass(env_exp(noise(ns(0.35, sr)), sr, 0.02, 0.12), 400, 3200, sr)
    for j in range(7):
        add(d, bubble(sr, amp=0.7), ns(rnd.uniform(0.02, 0.2), sr))
    W("dash", lowpass(d, 5000, sr), 0.75)

    # nibble: two small wet munches
    m = z(ns(0.3, sr))
    for j, at in enumerate((0.0, 0.11)):
        c = bandpass(env_exp(noise(ns(0.08, sr)), sr, 0.002, 0.02), 1300 + j * 300, 2.5, sr)
        add(m, c, ns(at, sr), 1.0)
        add(m, sweep_sine(500, 300, sr, 0.05, decay=0.015), ns(at, sr), 0.5)
    W("nibble", m, 0.7)

    # the line tick the Angler feels
    lt = z(ns(0.12, sr))
    for f, a in ((3100, 1.0), (4700, 0.5), (6900, 0.25)):
        add(lt, sine_note(f, sr, 0.0, attack=0.0005, release=0.05, amp=a))
    W("line_tick", env_exp(lt, sr, 0.0005, 0.02), 0.5)

    # strike / cast: whip crack
    wp = env_exp(noise(ns(0.25, sr)), sr, 0.003, 0.05)
    wp = highpass(sweep_lowpass(wp, 9000, 1500, sr), 600, sr)
    W("strike", wp, 0.8)

    # splashes
    def splash(dur, fc, nb):
        s = sweep_lowpass(env_exp(noise(ns(dur, sr)), sr, 0.004, dur * 0.3), fc, fc * 0.25, sr)
        for j in range(nb):
            add(s, bubble(sr, amp=0.5), ns(rnd.uniform(0.03, dur * 0.7), sr))
        return s
    W("splash_small", splash(0.45, 3000, 5), 0.6)
    W("splash", splash(0.9, 4500, 10), 0.85)

    # hooked: low twang + the fish's tiny gasp
    h = pluck(98, sr, 1.1, bright=0.8)
    h = env_exp(h, sr, 0.001, 0.35)
    add(h, sweep_sine(700, 1250, sr, 0.18, decay=0.08), ns(0.05, sr), 0.25)
    W("hooked", mix_reverb(h, sr, wet=0.25, tail=0.8), 0.85)

    # reel click and the tension whine loop (pitch driven by tension in Unity)
    rc = bandpass(env_exp(noise(ns(0.03, sr)), sr, 0.0003, 0.004), 3500, 3, sr)
    W("reel_click", rc, 0.45)
    wl = z(sr)
    for i in range(sr):
        t = i / sr
        f = 420 * (1 + 0.01 * math.sin(TAU * 6 * t))
        wl[i] = math.sin(TAU * 420 * t + 0.6 * math.sin(TAU * 6 * t)) * 0.6 + math.sin(TAU * 840 * t) * 0.18 + math.sin(TAU * 1260 * t) * 0.08
    W("whine_loop", wl, 0.5)

    # snap: high twang + crack
    sn = env_exp(pluck(330, sr, 0.8, bright=1.0), sr, 0.0005, 0.12)
    add(sn, highpass(env_exp(noise(ns(0.1, sr)), sr, 0.0005, 0.015), 1500, sr), 0, 1.2)
    W("snap", mix_reverb(sn, sr, wet=0.25, tail=0.6), 0.9)

    # landed: splash + deck thud
    ld = splash(0.7, 4000, 6)
    add(ld, sweep_sine(95, 45, sr, 0.35, decay=0.1), ns(0.18, sr), 1.4)
    add(ld, bandpass(env_exp(noise(ns(0.12, sr)), sr, 0.001, 0.03), 300, 2, sr), ns(0.18, sr), 1.2)
    W("landed", ld, 0.9)

    # conversion: muffled choral aah + a small cult ding + raincoat rustle
    cv = choir([midi(62), midi(69), midi(74)], sr, 0.9, attack=0.15, release=0.9)
    cv = lowpass(cv, 2200, sr)
    add(cv, bell(1318, sr, 1.6, index=1.5, ratio=3.5, amp=0.35), 0)
    ru = bandpass(noise(ns(0.6, sr)), 2500, 1.2, sr)
    for i in range(len(ru)):
        ru[i] *= (0.5 + 0.5 * math.sin(TAU * 23 * i / sr)) * math.exp(-i / sr / 0.25)
    add(cv, ru, ns(0.25, sr), 0.25)
    W("convert", mix_reverb(cv, sr, wet=0.5, size=1.2, tail=1.5), 0.85)

    # sonar ping: deep, with echoes everyone hears
    sp = sine_note(1040, sr, 0.0, attack=0.002, release=0.01)
    sp = z(ns(3.2, sr))
    for k, g in enumerate((1.0, 0.45, 0.22, 0.1)):
        p = sweep_sine(1040, 1000, sr, 1.4, decay=0.45, amp=g)
        add(sp, lowpass(p, 2500 - k * 500, sr), ns(k * 0.42, sr))
    W("sonar", mix_reverb(sp, sr, wet=0.45, size=1.3, tail=1.5), 0.8)

    # the Leviathan: growl swelling under a choir sting
    lv = z(ns(3.6, sr))
    ph = 0.0
    for i in range(len(lv)):
        t = i / sr
        f = 48 + 10 * math.sin(TAU * 0.4 * t)
        ph += f / sr
        ph -= int(ph)
        sw = math.sin(math.pi * min(1.0, t / 3.4))
        lv[i] = (2 * ph - 1) * sw * (0.6 + 0.4 * math.sin(TAU * 27 * t + rnd.random() * 0.3))
    lv = lowpass(lv, 300, sr, passes=2)
    add(lv, choir([midi(38), midi(50), midi(57), midi(62), midi(65)], sr, 2.4, attack=1.2, release=1.0, vowel=(600, 1000, 2600)), ns(0.4, sr), 0.6)
    W("leviathan", mix_reverb(lv, sr, wet=0.5, size=1.5, tail=2.0), 0.95)

    # the boat goes under: creak, gulp, bubbles
    bg = z(ns(2.4, sr))
    t = 0.0
    while t < 0.9:  # creaking wood: an irregular pulse train through a resonant band
        add(bg, bandpass(env_exp(noise(ns(0.02, sr)), sr, 0.0005, 0.004), 600 + 200 * math.sin(t * 9), 12, sr), ns(t, sr), 2.5)
        t += 0.012 + 0.03 * rnd.random() * (1 - t)
    add(bg, sweep_sine(220, 55, sr, 0.9, decay=0.4), ns(0.9, sr), 1.0)
    for j in range(16):
        add(bg, bubble(sr, f0=rnd.uniform(200, 500), amp=0.6), ns(rnd.uniform(1.0, 2.2), sr))
    W("gulp", lowpass(bg, 3000, sr), 0.9)

    # ink: wet pfft + blub
    ik = lowpass(env_exp(noise(ns(0.5, sr)), sr, 0.01, 0.12), 700, sr, passes=2)
    add(ik, sweep_sine(180, 90, sr, 0.25, decay=0.08), ns(0.03, sr), 0.6)
    W("ink", ik, 0.8)

    # bells: round start toll, round end, a small chime for marks and points
    W("round_start", mix_reverb(bell(146.8, sr, 4.0, index=3.5, ratio=1.41), sr, wet=0.5, size=1.4, tail=2.5), 0.85)
    re = bell(146.8, sr, 3.5, index=3.0, ratio=1.41)
    add(re, bell(110.0, sr, 4.0, index=3.0, ratio=1.41), ns(0.9, sr))
    W("round_end", mix_reverb(re, sr, wet=0.5, size=1.4, tail=2.5), 0.85)
    W("chime", mix_reverb(bell(1760, sr, 1.0, index=1.2, ratio=2.0, amp=0.5), sr, wet=0.4, tail=0.8), 0.45)
    W("points", mix_reverb(bell(1318.5, sr, 0.7, index=0.8, ratio=3.0, amp=0.4), sr, wet=0.3, tail=0.5), 0.4)

    # escape: whoosh + a flurry of bubbles
    es = sweep_lowpass(env_exp(noise(ns(0.6, sr)), sr, 0.05, 0.18), 300, 2500, sr)
    for j in range(12):
        add(es, bubble(sr, amp=0.6), ns(rnd.uniform(0.05, 0.45), sr))
    W("escape", es, 0.75)

    # phase change: a low swell (dusk) and a deeper, darker gong (night)
    W("dusk", mix_reverb(bell(98.0, sr, 4.0, index=2.0, ratio=1.5, amp=0.7), sr, wet=0.6, size=1.5, tail=3.0), 0.7)
    ng = choir([midi(38), midi(45), midi(50)], sr, 2.5, attack=1.5, release=2.0, vowel=(500, 900, 2400))
    add(ng, bell(73.4, sr, 5.0, index=2.5, ratio=1.41, amp=0.8), 0)
    W("night", mix_reverb(ng, sr, wet=0.6, size=1.6, tail=3.0), 0.75)

    # shock (electric fish part): a buzzy zap
    zp = z(ns(0.35, sr))
    for i in range(len(zp)):
        t = i / sr
        zp[i] = (1 if math.sin(TAU * (90 + 60 * math.sin(TAU * 31 * t)) * t) > 0 else -1) * math.exp(-t / 0.1) * (0.5 + 0.5 * (rnd.random() * 2 - 1))
    W("shock", lowpass(zp, 4000, sr), 0.6)


if __name__ == "__main__":
    which = set(sys.argv[2:]) if len(sys.argv) > 2 else {"sfx", "day", "night", "tension", "amb"}
    if "sfx" in which:
        print("sfx"); sfx()
    if "tension" in which:
        print("tension"); tension()
    if "amb" in which:
        print("ambience"); ambience_underwater(); ambience_waves()
    if "day" in which:
        print("day"); music_day()
    if "night" in which:
        print("night"); music_night()
    print("done ->", OUT)
