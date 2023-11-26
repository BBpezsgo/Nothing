using UnityEngine;

namespace Game.Components
{
    public class TeamRenderer : MonoBehaviour, ICopyable<TeamRenderer>
    {
        public MeshRenderer[] Renderers = new MeshRenderer[0];

        public void CopyTo(object destination) => this.CopyTo<TeamRenderer>(destination);
        public void CopyTo(TeamRenderer destination)
        {
            destination.Renderers = Renderers;
        }
    }
}
