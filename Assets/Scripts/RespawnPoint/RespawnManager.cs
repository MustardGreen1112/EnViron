using System.Collections.Generic;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    /* External object references */
    public List<VirusGenController> respawnPointControls;
    public MovementController virusController;

    /*
     * Called by virus when collision with killer T cell occurs
     * Respawns at a random active respawn point if available
     * If none are available, ends the game
     */
    public void PlayerKilled()
    {
        List<VirusGenController> validRespawns = new();
        foreach (VirusGenController respawn in respawnPointControls)
        {
            if (respawn.GetIsRespawn()) { validRespawns.Add(respawn); }
        }

        // Edge case: there are no valid respawns
        if (validRespawns.Count == 0) { Quit(); }

        int randomRespawn = Random.Range(0, validRespawns.Count);
        Vector3 respawnLoc = validRespawns[randomRespawn].transform.position;
        virusController.Respawn(respawnLoc);
    }

    void Quit() {
        #if UNITY_STANDALONE
            Application.Quit();
        #endif
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
