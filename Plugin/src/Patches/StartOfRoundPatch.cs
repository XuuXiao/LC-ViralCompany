using System.Collections;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ViralCompany.Patches;

[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch
{
	[HarmonyPatch(nameof(StartOfRound.Awake)), HarmonyPostfix]
	static void CreateUploadHandler(StartOfRound __instance)
	{
		__instance.StartCoroutine(SpawnUploadHandler());
	}

	static IEnumerator SpawnUploadHandler()
	{
		yield return new WaitUntil(() => StartOfRound.Instance.IsSpawned);
		if (!StartOfRound.Instance.IsHost)
		{
			yield break;
		}

		GameObject created = GameObject.Instantiate(Plugin.Assets.UploaderPrefab);
		SceneManager.MoveGameObjectToScene(created, StartOfRound.Instance.gameObject.scene);
		created.GetComponent<NetworkObject>().Spawn(true);
	}
}