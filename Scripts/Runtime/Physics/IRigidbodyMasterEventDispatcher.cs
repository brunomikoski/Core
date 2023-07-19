using System;
using System.Collections.Generic;

namespace BrunoMikoski.Core.PhysX
{
    public interface IRigidbodyMasterEventDispatcher
    {
        IList<RigidbodyWrapper> Slaves { get; }

        event Action<IRigidbodyMaster, RigidbodyWrapper> OnSlaveAddedEvent;
        event Action<IRigidbodyMaster, RigidbodyWrapper> OnSlaveRemovedEvent;

        void ReportSlaveAdded(RigidbodyWrapper wrapper);
        void ReportSlaveRemoved(RigidbodyWrapper wrapper);
    }
}