#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using SimVO.Audio;
using SimVO.Core;
using SimVO.Platform;
using SimVO.Targets;
using SimVO.UI;
using SimVO.Weapons;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SimVO.Testing
{
    /// <summary>Drives the desktop rig through every game scenario in play mode and reports PASS/FAIL lines.</summary>
    public class ScenarioRunner : MonoBehaviour
    {
        public static string Report { get; private set; } = "";
        public static bool Done { get; private set; }

        static ScenarioRunner instance;
        readonly List<string> lines = new List<string>();
        int passed;
        int failed;
        bool waitResult;

        public static void Run()
        {
            if (instance == null) instance = new GameObject("ScenarioRunner").AddComponent<ScenarioRunner>();
            Done = false;
            Report = "";
            instance.lines.Clear();
            instance.passed = instance.failed = 0;
            instance.StartCoroutine(instance.All());
        }

        GameSession S => GameSession.Instance;
        DesktopPlayer P => FindFirstObjectByType<DesktopPlayer>();
        GameMenu M => FindFirstObjectByType<GameMenu>();
        ControlPanel C => FindFirstObjectByType<ControlPanel>();
        TargetSpawner T => FindFirstObjectByType<TargetSpawner>();

        void Check(string name, bool ok, string detail = "")
        {
            lines.Add((ok ? "PASS " : "FAIL ") + name + (string.IsNullOrEmpty(detail) ? "" : " — " + detail));
            if (ok) passed++; else failed++;
            Report = string.Join("\n", lines);
        }

        IEnumerator Wait(float seconds)
        {
            var t = 0f;
            while (t < seconds) { t += Mathf.Min(Time.deltaTime, 0.1f * Time.timeScale); yield return null; }
        }

        // Editor hitches (Roslyn evals) produce multi-second frames; clamp so timeouts measure gameplay, not hiccups.
        static float Tick => Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        IEnumerator WaitReal(float seconds)
        {
            var t = 0f;
            while (t < seconds) { t += Tick; yield return null; }
        }

        IEnumerator Until(Func<bool> condition, float timeoutReal)
        {
            var t = 0f;
            waitResult = false;
            while (t < timeoutReal)
            {
                if (condition()) { waitResult = true; yield break; }
                t += Tick;
                yield return null;
            }
        }

        IEnumerator Arm()
        {
            P.TakeWeapon(0);
            yield return null;
            P.CurrentWeapon.QuickReload();
            yield return Wait(0.3f);
        }

        void Teleport(float x)
        {
            var controller = P.GetComponent<CharacterController>();
            controller.enabled = false;
            P.transform.position = new Vector3(x, P.transform.position.y, P.transform.position.z);
            controller.enabled = true;
        }

        string carrierTrace = "";

        IEnumerator CarrierAt(float distance)
        {
            var started = Time.realtimeSinceStartup;
            carrierTrace = "";
            var nextSample = 0f;
            var t = 0f;
            waitResult = false;
            while (t < 8f)
            {
                var inStage = T.Phase == $"stage@{distance}" || T.Phase == $"cover@{distance}";
                if (inStage && T.ActiveTarget != null && T.ActiveTarget.Armed || inStage && T.Phase.StartsWith("cover")) { if (!T.carrier.IsMoving && Mathf.Abs(T.carrier.CurrentDistance - distance) < 0.05f) { waitResult = true; break; } }
                if (t >= nextSample)
                {
                    carrierTrace += $"[{t:0.0}s d={T.carrier.CurrentDistance:0.0} mv={T.carrier.IsMoving} ph={T.Phase} hit={S.Hits}] ";
                    nextSample += 1f;
                }
                t += Tick;
                yield return null;
            }
            carrierTrace += $"real={Time.realtimeSinceStartup - started:0.0}s";
        }

        void Select(ShootingMode mode) { for (var i = 0; i < 4 && S.settings.mode != mode; i++) C.modeButton.Press(); }
        void Select(GameType game) { for (var i = 0; i < 3 && S.settings.gameType != game; i++) C.gameButton.Press(); }
        void Select(TargetKind kind) { for (var i = 0; i < 7 && S.settings.targetKind != kind; i++) C.targetButton.Press(); }
        void SelectMotion(bool dynamic) { for (var i = 0; i < 4 && S.settings.dynamicTargets != dynamic; i++) C.motionButton.Press(); }

        IEnumerator Shoot(Vector3 point)
        {
            P.AimAt(point);
            yield return null;
            P.CurrentWeapon.TryFire();
            yield return null;
        }

        Vector3 TargetCentre => T.ActiveTarget.transform.position;

        IEnumerator StopGame()
        {
            if (S.State == SessionState.Running) C.startButton.Press();
            yield return Until(() => !T.carrier.IsMoving && T.ActiveTarget != null && Mathf.Abs(T.carrier.CurrentDistance - T.practiceDistance) < 0.05f, 12f);
        }

        IEnumerator All()
        {
            yield return null; yield return null;
            try { Check("start menu shown and paused", M.State == GameMenu.MenuState.Start && S.Paused, $"state={M.State} paused={S.Paused}"); } catch (Exception e) { Check("start menu", false, e.Message); }

            // S1 start screen
            P.TakeWeapon(0);
            yield return null;
            Check("fire blocked while menu open", !P.CurrentWeapon.TryFire());
            M.playButton.Press();
            yield return null;
            Check("play hides menu and unpauses", M.State == GameMenu.MenuState.Hidden && !S.Paused);

            // S2 standard arcade ring across three distances
            Select(ShootingMode.Standard); Select(GameType.Arcade); Select(TargetKind.Ring); SelectMotion(false);
            Check("panel selections", S.settings.mode == ShootingMode.Standard && S.settings.gameType == GameType.Arcade && S.settings.targetKind == TargetKind.Ring && !S.settings.dynamicTargets);
            C.startButton.Press();
            yield return null;
            Check("start button runs game", S.State == SessionState.Running);
            foreach (var d in new[] { 5f, 10f, 15f })
            {
                yield return CarrierAt(d);
                Check($"carrier arrived at {d} m", waitResult, $"dist={T.carrier.CurrentDistance:0.00} moving={T.carrier.IsMoving} trace={carrierTrace}");
                var hitsBefore = S.Hits;
                yield return Shoot(TargetCentre);
                yield return Until(() => S.Hits == hitsBefore + 1, 1.5f);
                Check($"hit registered at {d} m", waitResult, $"hits={S.Hits} score={S.Score}");
            }
            Check("accuracy 100%", Mathf.Approximately(S.Accuracy, 1f), $"acc={S.Accuracy}");
            var score = S.Score;
            yield return StopGame();
            Check("stop button finishes game", S.State == SessionState.Finished || S.State == SessionState.Idle, $"state={S.State}");
            Check("record saved", PlayerPrefs.GetInt("simvo.best.Arcade.Standard", 0) >= score, $"best={S.BestScore}");

            // S3 pause during a timed game
            yield return Arm();
            Select(GameType.Timed);
            C.startButton.Press();
            yield return Wait(1f);
            var remaining = S.TimeRemaining;
            M.Toggle();
            yield return null;
            Check("esc opens pause menu", M.State == GameMenu.MenuState.Pause && S.Paused);
            yield return WaitReal(1f);
            Check("timer frozen while paused", Mathf.Abs(S.TimeRemaining - remaining) < 0.05f, $"before={remaining:0.00} after={S.TimeRemaining:0.00}");
            Check("fire blocked while paused", !P.CurrentWeapon.TryFire());
            Check("panel locked while paused", S.settings.gameType == GameType.Timed);
            M.resumeButton.Press();
            yield return null;
            Check("resume unpauses", !S.Paused && M.State == GameMenu.MenuState.Hidden);
            yield return StopGame();

            // S4 settings
            var music = Sfx.Music;
            M.Show(GameMenu.MenuState.Settings);
            M.musicDownButton.Press();
            Check("music volume decreases", Mathf.Abs(Sfx.Music - Mathf.Clamp01(music - 0.1f)) < 0.001f, $"music={Sfx.Music}");
            Check("music volume persisted", Mathf.Abs(PlayerPrefs.GetFloat("simvo.volume.music") - Sfx.Music) < 0.001f);
            var player = FindFirstObjectByType<MusicPlayer>();
            Check("music player follows setting", player != null && Mathf.Abs(player.GetComponent<AudioSource>().volume - Sfx.Music * player.baseVolume) < 0.001f && player.GetComponent<AudioSource>().isPlaying);
            M.musicUpButton.Press();
            var effects = Sfx.Effects;
            M.effectsDownButton.Press();
            Check("effects volume decreases", Mathf.Abs(Sfx.Effects - Mathf.Clamp01(effects - 0.1f)) < 0.001f);
            M.effectsUpButton.Press();
            M.backButton.Press();
            Check("back returns to start screen", M.State == GameMenu.MenuState.Start);
            M.playButton.Press();

            // S5 main menu resets a running game
            Select(GameType.Arcade);
            C.startButton.Press();
            yield return Wait(0.5f);
            M.Show(GameMenu.MenuState.Pause);
            M.mainMenuButton.Press();
            Check("main menu resets session", S.State == SessionState.Idle && M.State == GameMenu.MenuState.Start, $"state={S.State}");
            M.playButton.Press();
            yield return StopGame();

            // S6 timed game runs out
            Select(GameType.Timed);
            C.startButton.Press();
            Time.timeScale = 8f;
            yield return Until(() => S.State == SessionState.Finished, 20f);
            Time.timeScale = 1f;
            Check("timed game finishes at zero", waitResult && S.TimeRemaining <= 0.01f, $"state={S.State} left={S.TimeRemaining:0.0}");
            yield return StopGame();

            // S7 advanced positions
            yield return Arm();
            Select(ShootingMode.Advanced); Select(GameType.Arcade); Select(TargetKind.Ring);
            P.SetPosition(BodyPosition.Standing);
            C.startButton.Press();
            yield return CarrierAt(5f);
            yield return Shoot(TargetCentre);
            yield return Until(() => S.Hits == 1, 1.5f);
            Check("advanced: standing hit at 5 m", waitResult);
            yield return CarrierAt(10f);
            yield return null;
            Check("advanced: kneeling required at 10 m", S.RequiredPosition == BodyPosition.Kneeling && !S.PositionSatisfied && T.StageHint.Contains("колена"), $"hint={T.StageHint}");
            yield return Shoot(TargetCentre);
            yield return Wait(0.6f);
            Check("advanced: no score in wrong position", S.Hits == 1, $"hits={S.Hits}");
            P.SetPosition(BodyPosition.Kneeling);
            yield return Wait(0.2f);
            yield return Shoot(TargetCentre);
            yield return Until(() => S.Hits == 2, 1.5f);
            Check("advanced: kneeling hit counts", waitResult, $"hits={S.Hits}");
            yield return CarrierAt(15f);
            P.SetPosition(BodyPosition.Prone);
            yield return Wait(0.2f);
            yield return Shoot(TargetCentre);
            yield return Until(() => S.Hits == 3, 1.5f);
            Check("advanced: prone hit at 15 m", waitResult, $"hits={S.Hits}");
            P.SetPosition(BodyPosition.Standing);
            yield return StopGame();

            // S8 cover mode: the shooter leans to the window on the right
            yield return Arm();
            Teleport(0.45f);
            var barricade = FindFirstObjectByType<CoverBarricade>();
            Select(ShootingMode.Cover);
            yield return Wait(1.3f);
            Check("barricade rises in cover mode", barricade != null && barricade.rising.localPosition.y > -0.01f, $"y={barricade?.rising.localPosition.y}");
            C.startButton.Press();
            yield return CarrierAt(5f);
            yield return Until(() => !T.TargetExposed, 3f);
            Check("cover: target hides", waitResult);
            yield return Until(() => T.TargetExposed, 8f);
            Check("cover: target shows itself", waitResult, $"hint={T.StageHint}");
            yield return Wait(0.3f);
            yield return Shoot(TargetCentre);
            yield return Until(() => S.Hits == 1, 1.5f);
            Check("cover: hit during exposure counts", waitResult, $"score={S.Score}");
            yield return CarrierAt(10f);
            yield return Until(() => T.TargetExposed, 8f);
            yield return Until(() => S.MissedExposures == 1, 8f);
            Check("cover: missed exposure counted", waitResult, $"missed={S.MissedExposures}");
            Check("cover: exposure metric runs", S.CoverTotalTime > 0.5f, $"total={S.CoverTotalTime:0.0} ratio={S.CoverRatio:0.00}");
            yield return StopGame();
            Select(ShootingMode.Standard);
            yield return Wait(1.3f);
            Check("barricade lowers in standard mode", barricade.rising.localPosition.y < -2f, $"y={barricade.rising.localPosition.y}");
            Teleport(0f);

            // S9 target kinds
            yield return Arm();
            Select(TargetKind.Silhouette);
            C.startButton.Press();
            yield return CarrierAt(5f);
            yield return Shoot(TargetCentre + new Vector3(0f, 0.27f, 0f));
            yield return Until(() => S.Hits == 1, 1.5f);
            Check("silhouette: head zone scores 5", waitResult && S.Score == 5, $"score={S.Score}");
            yield return StopGame();

            yield return Arm();
            Select(TargetKind.Gong);
            C.startButton.Press();
            yield return CarrierAt(5f);
            var gong = T.ActiveTarget as GongTarget;
            yield return Shoot(TargetCentre);
            yield return Until(() => S.Hits == 1, 1.5f);
            Check("gong: centre scores 10", waitResult && S.Score == 10, $"score={S.Score}");
            yield return Wait(0.15f);
            Check("gong: swings after hit", gong != null && Quaternion.Angle(gong.swingPivot.localRotation, Quaternion.identity) > 1f);
            yield return StopGame();

            yield return Arm();
            Select(TargetKind.Hologram);
            C.startButton.Press();
            yield return CarrierAt(5f);
            yield return Shoot(TargetCentre);
            yield return Until(() => S.Hits == 1, 1.5f);
            Check("hologram: hit scores", waitResult && S.Score >= 9, $"score={S.Score}");
            yield return StopGame();

            Select(TargetKind.Clay);
            P.TakeWeapon(1);
            C.startButton.Press();
            yield return Until(() => FindFirstObjectByType<ClayTarget>() != null, 5f);
            Check("clay: thrower launches a clay", waitResult);
            var clay = FindFirstObjectByType<ClayTarget>();
            P.CurrentWeapon.Cock();
            Time.timeScale = 0.05f;
            yield return WaitReal(0.3f);
            var clayPos = clay.transform.position;
            var rifle = P.CurrentWeapon;
            var sway = (rifle.swayStanding, rifle.swayKneeling, rifle.swayProne);
            rifle.swayStanding = rifle.swayKneeling = rifle.swayProne = 0f;
            yield return Shoot(clay.transform.position);
            (rifle.swayStanding, rifle.swayKneeling, rifle.swayProne) = sway;
            var muzzle = P.CurrentWeapon.muzzle;
            var probe = Physics.Raycast(muzzle.position, muzzle.forward, out var probeHit, 60f, SimVOLayers.ProjectileHitMask, QueryTriggerInteraction.Ignore) ? probeHit.collider.name + "@" + probeHit.distance.ToString("0.0") : "nothing";
            yield return Until(() => S.Hits == 1, 2f);
            Time.timeScale = 1f;
            Check("clay: shot in flight counts", waitResult, $"hits={S.Hits} score={S.Score} clayPos={clayPos:0.0} rounds={P.CurrentWeapon.RoundsLoaded} muzzleRay={probe} clayAlive={(clay != null)}");
            yield return Until(() => S.MissedExposures >= 1, 10f);
            Check("clay: untouched clay counted as missed", waitResult, $"missed={S.MissedExposures}");
            yield return StopGame();
            Select(TargetKind.Ring);

            // S10 dynamic motion
            SelectMotion(true);
            var mount = T.carrier.targetMount;
            yield return Wait(1f);
            var x0 = mount.localPosition.x;
            yield return Wait(0.7f);
            Check("dynamic: target drifts sideways", Mathf.Abs(mount.localPosition.x - x0) > 0.05f, $"dx={mount.localPosition.x - x0:0.000}");
            SelectMotion(false);

            // S11 weapons (honest mode: cocking required)
            var assist = Sfx.Assist;
            Sfx.Assist = false;
            P.TakeWeapon(0);
            var pistol = P.CurrentWeapon as Pistol;
            pistol.Cock();
            pistol.EjectMagazine();
            yield return Wait(0.5f);
            Check("pistol: eject empties the gun", pistol.Magazine == null && !pistol.HasAmmo && !pistol.TryFire());
            pistol.QuickReload();
            yield return Wait(0.3f);
            Check("pistol: quick reload inserts a full magazine", pistol.Magazine != null && pistol.RoundsLoaded == 8, $"rounds={pistol.RoundsLoaded}");
            P.TakeWeapon(2);
            var shotgun = P.CurrentWeapon as Shotgun;
            shotgun.Cock();
            shotgun.TryFire();
            yield return Wait(0.8f);
            Check("shotgun: shot consumes a shell", shotgun.ShellsLoaded == 4, $"shells={shotgun.ShellsLoaded}");
            Check("shotgun: pump needed before the next shot", !shotgun.Chambered);
            var shell = FindFirstObjectByType<Shell>();
            if (shell != null)
            {
                var port = shotgun.loadingPort;
                // A hand would hold the shell; here the box socket is switched off so it cannot pull the shell back.
                XRSocketInteractor box = null;
                if (shell.Grab.isSelected)
                {
                    box = shell.Grab.firstInteractorSelecting as XRSocketInteractor;
                    if (box != null) box.socketActive = false;
                    port.interactionManager.SelectExit(shell.Grab.firstInteractorSelecting, (IXRSelectInteractable)shell.Grab);
                }
                var shellBody = shell.GetComponent<Rigidbody>();
                shellBody.isKinematic = true;
                shell.transform.SetParent(port.transform, false);
                shell.transform.localPosition = Vector3.zero;
                yield return Until(() => port.IsHovering(shell.Grab), 1f);
                if (!waitResult) Check("shotgun: port hovers a shell placed in it", false, $"no hover, shellPos={shell.transform.position:0.00} port={port.transform.position:0.00}");
                port.interactionManager.SelectEnter((IXRSelectInteractor)port, (IXRSelectInteractable)shell.Grab);
                if (box != null) box.socketActive = true;
                yield return Wait(0.3f);
                Check("shotgun: shell in the port loads a round", shotgun.ShellsLoaded == 5 && shell == null, $"shells={shotgun.ShellsLoaded}");
            }
            else Check("shotgun: shell available on the bench", false);
            P.TakeWeapon(1);
            var carbine = P.CurrentWeapon as Carbine;
            var rounds = carbine.RoundsLoaded;
            carbine.Cock();
            Check("carbine: fires", carbine.TryFire() && carbine.RoundsLoaded == rounds - 1);
            yield return Wait(0.6f);
            Check("carbine: bolt action needs cocking after a shot", !carbine.Chambered && !carbine.TryFire());
            carbine.Cock();
            Check("carbine: cocking chambers the next round", carbine.Chambered);
            Check("sounds: recorded clips assigned", SfxLibrary.RifleShot != ProceduralSfx.RifleShot && SfxLibrary.PistolShot != ProceduralSfx.PistolShot);
            foreach (var index in new[] { 3, 4, 5, 6 })
            {
                P.TakeWeapon(index);
                var gun = P.CurrentWeapon;
                gun.Cock();
                var before = gun.RoundsLoaded;
                Check($"{gun.displayName}: fires", gun.TryFire() && gun.RoundsLoaded == before - 1, $"rounds={gun.RoundsLoaded}");
                yield return Wait(0.3f);
            }
            Sfx.Assist = assist;

            // S12 dispenser respawn
            MagazineDispenser dispenser = null;
            foreach (var d in FindObjectsByType<MagazineDispenser>(FindObjectsSortMode.None)) if (d.itemPrefab.GetComponent<Magazine>() != null) dispenser = d;
            if (dispenser != null && dispenser.socket.hasSelection)
            {
                var taken = dispenser.socket.firstInteractableSelected;
                var takenGrab = taken as XRGrabInteractable;
                takenGrab.enabled = false;
                taken.transform.position += new Vector3(0f, 0.5f, -0.6f);
                yield return Wait(1.2f);
                Check("dispenser: new magazine appears after taking one", dispenser.socket.hasSelection && dispenser.socket.firstInteractableSelected != taken, $"hasSel={dispenser.socket.hasSelection}");
                takenGrab.enabled = true;
            }
            else Check("dispenser: holds a magazine", false);

            lines.Add($"DONE passed={passed} failed={failed}");
            Report = string.Join("\n", lines);
            Done = true;
        }
    }
}
#endif
