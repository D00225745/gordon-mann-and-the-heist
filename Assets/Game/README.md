# Gordon Mann in The Vault Job

Open **Tools → Scene Builder → Open Vault Job Blockout** in Unity and press Play.
The Vault title screen opens directly to Play, Outcomes, Sound, and Quit.
Outcomes tracks the 13 failure gags and the winning escape.

Use **Tools → Scene Builder → Build Gordon Mann Windows Playtest** to package
the current blockout scenes. Launch `Gordon Mann.exe` from the build folder
with its supporting files alongside it.

Stickmin-style branching game. Every story beat is its own scene with a
SceneDirector on it: the intro cutscene plays, then the beat either shows
choices, the fail screen, the win screen, or auto-advances.

Current flow ("The Vault Job"): MainMenu > Choice_1..5 > Victory, with a fail
scene for every wrong option. Wrong picks show YOU FAILED / RETRY; retry jumps
back to the choice you came from. Choice_4 has an 8s countdown.

## The Vault Job blockouts

Open `Assets/Game/Blockout/Scenes` to edit the 20 blockout scenes. Each has a
`Vault Job Blockout` object with **Architecture**, **Props**, and **Staging
Markers**. The markers give you positions for Gordon, the camera, scene
entrances and exits, and the important beats of each gag. Use the existing
SceneDirector and its Intro Timeline field when animating a scene.

To play the full route through these sets, choose **Tools → Scene Builder →
Open Vault Job Blockout** and press Play. **Use Original Vault Job Scenes** switches the build list
back to the prototype. The blockout generator creates missing copies and
skips copies that already exist.

| Decision scene | Layout and staging |
| --- | --- |
| Choice_1 | Bank façade, front steps, Portal Jar and portal landing |
| Choice_2 | Reception desk, guard positions, fountain and route to security |
| Choice_3 | Steel door and pivot, keypad, Library Card and doorway |
| Choice_4 | Five laser rows, a floor-slide lane with entry and exit markers, and a vault door |
| Choice_5 | Service exit, curb, bus stop, and bus arrival, boarding and departure marks |

The 13 failure scenes share those locations and have editable props or
staging positions for their punchlines. Victory uses the bus stop set.

### Failed break-ins

At the bank, try **Pogo Stick**, **Pizza Guy**, or **Cannonball**. Each
plays a short scene with the v005 character and sound cues before showing
Retry. The sequence object in each failure scene holds the actor, prop,
and camera marks. The clips are under `Blockout/Bank Failures`.

### Portal Jar

In the blockout, pick **Portal Jar** at the bank. The jar drops, the portal
opens, and Gordon slides into the lobby. The lobby waits for his arrival
before showing its choices. Retrying a lobby failure returns to those choices.

The character asset in `Blockout/Portal Jar` contains poses baked from the
v005 Maya rig, including its evaluated skin and corrective shapes. The
`Portal Jar Sequence` objects in `Choice_1` and `Choice_2` hold the staging
marks, portal geometry, and sound clips. The Maya rig remains the source for
editing or baking additional poses.

### Lullabies

After taking the Portal Jar into the lobby, pick **Lullabies**. Gordon reaches
for the player, waits through the two guards' reactions, then tiptoes to the
security hall. The scene advances to **The Security Door**.

The lobby's `Lullabies Sequence` holds the guard pivots, three camera marks,
and Gordon's route. The character poses are baked from the v005 rig, including
separate left and right stepping poses. The four clips in `Blockout/Lullabies`
provide the player click, music-box melody, guard slump, and footsteps.

### Failed lobby choices

**Statue** holds on Gordon's disguise until he sneezes. **Rolling Chair**
sends him into the fountain. **Fire Alarm** brings a crew into the lobby.
All three return to the lobby choices through Retry. Their v005 poses,
editable office chair and crew prefabs, and sound cues are in
`Blockout/Lobby Failures`.

### Library Card

Choose **Library Card** at the security door to play the card scan, green
approval light, and sliding door reveal. Gordon crosses the threshold before
the game loads the laser hallway. The eight-second countdown starts when the
hallway choices become available.

The reusable reader and sound effects are in `Blockout/Library Card`. The
`Library Card Sequence` in `Choice_3` holds the camera and actor positions.

### Failed security door choices

**Guess the PIN** plays four key presses and a rejection. **Knock Politely**
opens the door on a guard midway through a coffee break. Both scenes wait for
the gag before offering Retry. The numbered keypad and mug prefabs, v005
pose export, and sound cues are under `Blockout/Security Failures`.

### Floor Slide

Choose **Floor Slide** before the hallway timer expires. Gordon plants his
feet, drops low, slides under the five beams, and stands back up before the
cut to **The Getaway**. The animation and sound assets are under
`Blockout/Floor Slide`. The pose export includes the v005 skin and evaluated
correctives. Camera marks and the slide path are editable in `Choice_4`.

### Failed laser hallway choices

**Mail Cart** rolls up to the first beam, **Unplug It** cuts the room lights
while the beams stay on, and **Selfie First** triggers the warning with a
phone flash. Let the eight-second timer expire to see the beam sweep toward
Gordon. Retry starts a fresh countdown. The mail trolley, lighting panel,
phone, pose export, and sound clips are in `Blockout/Laser Failures`.

### Night Bus

Choose **Night Bus** in **The Getaway** to finish the winning route. Gordon
waits at the curb, pays his fare, and boards. Victory appears after the doors
close and the bus drives away. The reusable bus prefab has four wheels,
sliding door leaves, a fare box, and working lights represented by materials.
It and the sound cues are under `Blockout/Night Bus`.

### Rocket Skates

Choose **Rocket Skates** at the getaway. Gordon leans into the first push,
wobbles on the sidewalk, and fails to stop at a barricade before Retry.
The skates are fitted to the v005 shoe mesh. The editable skate, striped
barricade, and traffic cone prefabs are in `Blockout/Rocket Skates`.

## Adding a beat

- Tools > Scene Builder > New Scene... creates a fully wired scene, or copy an
  existing one.
- On its SceneDirector: pick the Outcome, fill in the choices (label / icon /
  target scene name). Target scenes must be in the build list — the first
  scene there is where MAIN MENU / restart lands.

## Cutscenes

- Put a Timeline in the scene and assign it to Intro Timeline on the
  SceneDirector. The outcome fires when it finishes (don't loop it).
- No timeline assigned: introDelay seconds pass instead (0 = instant).
- FinishIntro() can be called from an animation event to cut it short.
- onIntroFinished fires right before the outcome — handy for sounds/cuts.

## Choice icons

Import an image as Sprite (2D and UI) and drop it in the choice's icon field.
No icon = label only.

## Rebuilding

Tools > Scene Builder > Rebuild All Scenes regenerates every scene from
SceneBuilder.cs. Careful: it overwrites any hand-made edits in those scenes,
so once scenes get real set dressing, stop rebuilding and edit them directly.

