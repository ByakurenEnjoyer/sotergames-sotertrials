using Sandbox;

public sealed class Timer : Component

{
    [Property] public BoxCollider StartLine { get; set; }
    [Property] public BoxCollider FinishLine { get; set; }


    public TimeSince Time;
    public float FinalTime;

    bool _isPaused = true;

    public void StartTimer()
    {
        Time = 0;
        _isPaused = false;
        FinalTime = 0;
    }
    public void StopTimer()
    {
        _isPaused = true;
    }

    protected override void OnStart()
    {
        StartLine.OnTriggerEnter += OnTriggerEnter;
        FinishLine.OnTriggerEnter += OnOtherTriggerEnter;
        FinishLine.OnTriggerExit += OnOtherTriggerEnter;
    }

    void OnTriggerEnter( Collider StartLine )
    {
        if ( StartLine.Components.TryGet( out PlayerController pc ) )
            StartTimer();
    }

    void OnOtherTriggerEnter( Collider FinishLine )
    {
        if ( FinishLine.Components.TryGet( out PlayerController pc ) )
            StopTimer();
    }
    void OnTriggerExit( Collider other )
    {

    }

    protected override void OnUpdate()
    {
        
        if ( _isPaused ) return;
        FinalTime = Time;
    }

}