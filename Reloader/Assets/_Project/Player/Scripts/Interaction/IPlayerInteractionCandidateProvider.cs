namespace Reloader.Player.Interaction
{
    public interface IPlayerInteractionCandidateProvider
    {
        bool TryGetInteractionCandidate(out PlayerInteractionCandidate candidate);
    }
}
