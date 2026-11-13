using UnityEngine;

public interface IInteractable
{
    string Prompt { get; }                 // e.g. "Talk"
    void Interact(PlayerInteractor who);
}
// made by pradhyot. N


