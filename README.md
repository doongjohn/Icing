# Icing

Some useful scripts for Unity game development

## BPCC

Box Platformer Character Controller.

- Uses Rigidbody2D and BoxCollider2D.
- Uses AnimationCurve for jump time and height.
- Snap to ground while walking on slope. (no bouncing.)
- Supports one way platform.

[![BPCC Video](https://i.imgur.com/1YLaxbR.png)](https://www.youtube.com/watch?v=INcRnxI3td4&feature=youtu.be)

## GSM

Glorified State Machine.

Example code

```cs
public class NPC : GSM_Controller
{
    private Transform target;
    private float targetDist;
    private int health = 10;

    protected void Awake()
    {
        target = GameObject.Find("Target").transform;

        // Get states
        gameObject.GetComponent(out NPC_Death state_death);
        gameObject.GetComponent(out NPC_Idle state_idle);
        gameObject.GetComponent(out NPC_Follow state_follow);
        gameObject.GetComponent(out NPC_RunAway state_runAway);

        // Init Bvr
        var bvr_Death = new BvrSingle(
            stateEx: null,      // Extra Data for transitioning state + Additional state action.
            state: state_death, // state_death will be executed when this Bvr is currently active.
            isDone: () => false // If this returns true, then this Bvr is finished.
        );
        var bvr_follow = new BvrSingle(
            stateEx: null,
            state: state_follow,
            isDone: () => targetDist <= 5f
        );
        var bvr_runAway = new BvrSingle(
            stateEx: null,
            state: state_runAway,
            isDone: () => targetDist >= 3f
        );
        var bvr_attack = new BvrSingle(
            stateEx: null,
            state: state_follow,
            isDone: () => targetDist <= 0.5f
        );

        // Init Flow
        var flow_normal = new Flow();
        var flow_angry = new Flow();

        // Define Flow Logic
        // "flow_begin" will be always checked before checking the current flow.
        flow_begin
            .ForceDo(() => health <= 0, bvr_Death); // ForceDo() and ForceTo() will be always checked even when Bvr.Wait() is not finished.

        flow_normal
            .To(() => Input.GetKeyDown(KeyCode.Return) && !bvr_attack.IsFinished, flow_angry) // Press Enter to change current flow to flow_angry.
            .Do(() => targetDist < 3f, bvr_runAway)
            .Do(() => targetDist > 5f, bvr_follow);

        flow_angry
            .Do(() => true, bvr_attack.Wait()  // Don't check Do(), To() until bvr_attack is finished.
                .To(() => true, flow_normal)); // Change current flow to flow_normal after bvr_attack is finished.

        // Init GSM
        GSM_Init(
            startingFlow: flow_normal,
            defaultStateEx: null,
            defaultState: state_idle
        );
    }
    protected override void Update()
    {
        base.Update();
        targetDist = Vector2.Distance(target.position, transform.position);
    }
}
```

## Helpers

- Helper extension methods
- Singleton
- Timer

## TODO

- Add Object pooling
- Add UI Components
- Add more useful scripts
- Fix bugs
- Use [OpenUPM](https://openupm.com) format
