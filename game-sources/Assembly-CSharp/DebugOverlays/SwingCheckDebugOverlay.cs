using UnityEngine;

namespace DebugOverlays;

public class SwingCheckDebugOverlay : MonoBehaviour
{
	private bool damaging;

	private EnemyIdentifier eid;

	public void ConsumeData(bool damaging, EnemyIdentifier eid)
	{
		this.damaging = damaging;
		this.eid = eid;
	}

	private void OnGUI()
	{
		if (!damaging)
		{
			return;
		}
		Rect? onScreenRect = OnGUIHelper.GetOnScreenRect(base.transform.position);
		if (onScreenRect.HasValue)
		{
			Rect value = onScreenRect.Value;
			GUI.Label(value, "SWING!", new GUIStyle
			{
				fontSize = 20,
				fontStyle = FontStyle.Bold,
				normal = 
				{
					textColor = Color.red
				}
			});
			value.y += 20f;
			if (eid == null)
			{
				GUI.Label(value, "No EID", new GUIStyle
				{
					fontSize = 20,
					fontStyle = FontStyle.Bold,
					normal = 
					{
						textColor = Color.magenta
					}
				});
			}
			else if (eid.target == null)
			{
				GUI.Label(value, "No target", new GUIStyle
				{
					fontSize = 20,
					fontStyle = FontStyle.Bold,
					normal = 
					{
						textColor = Color.yellow
					}
				});
			}
			else if (eid.target.isPlayer)
			{
				GUI.Label(value, "Player target", new GUIStyle
				{
					fontSize = 20,
					fontStyle = FontStyle.Bold,
					normal = 
					{
						textColor = Color.green
					}
				});
			}
			else
			{
				GUI.Label(value, eid.target.ToString(), new GUIStyle
				{
					fontSize = 20,
					fontStyle = FontStyle.Bold,
					normal = 
					{
						textColor = Color.blue
					}
				});
			}
		}
	}
}
