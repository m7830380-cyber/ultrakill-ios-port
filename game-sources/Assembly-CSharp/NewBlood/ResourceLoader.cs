using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NewBlood;

internal static class ResourceLoader
{
	public static IEnumerator LoadAudioClip(string path, AudioClipLoadType loadType, Action<AudioClip> onCompleted)
	{
		return LoadAudioClip(path, loadType, AudioType.UNKNOWN, onCompleted, (Action<AudioClip> _, AudioClip clip) =>
		{
			onCompleted(clip);
		});
	}

	public static IEnumerator LoadAudioClip(string path, AudioClipLoadType loadType, AudioType audioType, Action<AudioClip> onCompleted)
	{
		return LoadAudioClip(path, loadType, audioType, onCompleted, (Action<AudioClip> _, AudioClip clip) =>
		{
			onCompleted(clip);
		});
	}

	public static IEnumerator LoadAudioClip<TState>(string path, AudioClipLoadType loadType, TState state, Action<TState, AudioClip> onCompleted)
	{
		return LoadAudioClip(path, loadType, AudioType.UNKNOWN, state, onCompleted);
	}

	public static IEnumerator LoadAudioClip<TState>(string path, AudioClipLoadType loadType, AudioType audioType, TState state, Action<TState, AudioClip> onCompleted)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (onCompleted == null)
		{
			throw new ArgumentNullException("onCompleted");
		}
		Uri fileUri = GetFileUri(path);
		UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fileUri, audioType);
		DownloadHandlerAudioClip handler = (DownloadHandlerAudioClip)request.downloadHandler;
		switch (loadType)
		{
		case AudioClipLoadType.Streaming:
			handler.streamAudio = true;
			break;
		case AudioClipLoadType.CompressedInMemory:
			handler.compressed = true;
			break;
		}
		UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = request.SendWebRequest();
		DisposeAndThrowIfRequestFailed(request);
		if (loadType == AudioClipLoadType.Streaming)
		{
			try
			{
				onCompleted(state, handler.audioClip);
			}
			catch
			{
				request.Dispose();
				throw;
			}
		}
		yield return unityWebRequestAsyncOperation;
		DisposeAndThrowIfRequestFailed(request);
		if (loadType == AudioClipLoadType.Streaming)
		{
			request.Dispose();
			yield break;
		}
		using (request)
		{
			onCompleted(state, handler.audioClip);
		}
	}

	private static void DisposeAndThrowIfRequestFailed(UnityWebRequest request)
	{
		if (!request.isHttpError || !request.isNetworkError || !request.isDone)
		{
			return;
		}
		Exception exceptionForWebRequest = GetExceptionForWebRequest(request);
		request.Dispose();
		throw exceptionForWebRequest;
	}

	private static Exception GetExceptionForWebRequest(UnityWebRequest request)
	{
		if (request.responseCode == 404)
		{
			return new FileNotFoundException(null, request.uri.LocalPath);
		}
		return new Exception(request.error);
	}

	private static Uri GetFileUri(string path)
	{
		path = Path.GetFullPath(path);
		path = path.Replace('\\', '/');
		path = Uri.EscapeUriString(path);
		return new UriBuilder(Uri.UriSchemeFile, string.Empty, 0, path).Uri;
	}
}
