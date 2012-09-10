using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiftSimulator
{
    public enum ElevatorStatus
    {
        Idle,
        PreparingForJob,
        GoingUp,
        GoingDown,
        WaitingForPassengersToGetInAndGetOut,
    }
}
