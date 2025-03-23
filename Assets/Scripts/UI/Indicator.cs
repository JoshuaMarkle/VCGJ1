using UnityEngine;
using TMPro;

public class Indicator : MonoBehaviour
{
	[Header("Indicator Settings")]
	public Transform target;
	public float screenPaddingX = 50f;
	public float screenPaddingY = 100f;

	private RectTransform indicatorRect;
	private Camera mainCamera;
	private TMP_Text indicatorText;

	private void Start()
	{
		// Search for UI
		if (UI.Instance)

			mainCamera = Camera.main;
		indicatorRect = GetComponent<RectTransform>();
		indicatorText = GetComponent<TMP_Text>();
	}

	private void Update()
	{
		if (target == null || mainCamera == null)
			return;

		// Convert the target's world position into screen space.
		Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);

		// If the target is behind the camera, flip the screen position.
		if (screenPos.z < 0)
		{
			screenPos *= -1;
		}

		// Clamp the screen position so the indicator stays within the screen with the given padding.
		screenPos.x = Mathf.Clamp(screenPos.x, screenPaddingX, Screen.width - screenPaddingX);
		screenPos.y = Mathf.Clamp(screenPos.y, screenPaddingY, Screen.height - screenPaddingY);

		// Update the indicator's position.
		indicatorRect.position = screenPos;
	}

	public void UpdateIndicatorText(string newText)
	{
		if (indicatorText != null)
		{
			indicatorText.text = newText;
		}
	}

	public void SetTextColor(Color color)
	{
		if (indicatorText != null)
			indicatorText.color = color;
	}
}
