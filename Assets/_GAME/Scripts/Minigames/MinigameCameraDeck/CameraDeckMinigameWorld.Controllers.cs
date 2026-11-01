using UnityEngine;

namespace Minigames
{
    public partial class CameraDeckMinigameWorld
    {
        private interface IModeController
        {
            void Tick(float deltaTime);
            void ClickPlan(Vector3 point);
            void RightClickPlan();
        }

        private abstract class ModeController : IModeController
        {
            protected readonly CameraDeckMinigameWorld World;

            protected ModeController(CameraDeckMinigameWorld world)
            {
                World = world;
            }

            public abstract void Tick(float deltaTime);
            public abstract void ClickPlan(Vector3 point);
            public virtual void RightClickPlan() { }

            protected void SelectMount(Vector3 point)
            {
                int mountIndex = World.FindMountNear(point);
                if (mountIndex >= 0)
                    World.SelectCamera(mountIndex);
            }
        }

        private sealed class AuditController : ModeController
        {
            public AuditController(CameraDeckMinigameWorld world) : base(world) { }

            public override void Tick(float deltaTime)
            {
                World.HandleCameraKeys();
                if (World.testRunning)
                    World.UpdateTestRun(deltaTime);
            }

            public override void ClickPlan(Vector3 point)
            {
                SelectMount(point);
            }
        }

        private sealed class SweepController : ModeController
        {
            public SweepController(CameraDeckMinigameWorld world) : base(world) { }

            public override void Tick(float deltaTime)
            {
                World.HandleCameraKeys();
            }

            public override void ClickPlan(Vector3 point)
            {
                SelectMount(point);
            }
        }

        private sealed class RouteController : ModeController
        {
            public RouteController(CameraDeckMinigameWorld world) : base(world) { }

            public override void Tick(float deltaTime)
            {
                World.UpdateSweep(deltaTime);
                World.conesDirty = true;

                if (Input.GetKeyDown(KeyCode.Space))
                    World.StopRouteMovement("Movement stopped.");

                World.UpdateRouteMode(deltaTime);
            }

            public override void ClickPlan(Vector3 point)
            {
                World.SetRouteDestination(point);
            }

            public override void RightClickPlan()
            {
                World.StopRouteMovement("Movement stopped.");
            }
        }

        private IModeController modeController;

        private IModeController CreateModeController()
        {
            switch (minigame.Mode)
            {
                case CameraDeckMode.BlindSpotAudit:
                    return new AuditController(this);
                case CameraDeckMode.CleanDeskSweep:
                    return new SweepController(this);
                default:
                    return new RouteController(this);
            }
        }
    }
}
