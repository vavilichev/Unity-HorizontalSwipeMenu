using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class HorizontalSwipeMenu : MonoBehaviour, IBeginDragHandler, IEndDragHandler {

	[Tooltip("Index of item snapped by default")]
	public int defaultItemIndex;
	[Tooltip("Maximum swipe diration for detecting swipe")]
	public float swipeDuration = 0.3f;

	[Space(6)]
	[Range(0f, 1f)]
	[Tooltip("Minimum part of screen distance for detecting swipe")]
	public float distanceToSwipe = 0.2f;

	[Space(6)]
	[Tooltip("If you using HorizontalLayoutGroup, this property must repeats the same in the specified script")]
	public float spacing = 0f;
	[Tooltip("Time for snapping item to position")]
	public float snapDuration = 0.2f;

	[Space(6)]
	public Button leftBtn;
	public Button rightBtn;

	[Space(6)]
	public bool initializeOnAwake;


	private Canvas canvas;
	private RectTransform mainTransform;
	private ScrollRect scrollRect;
	private RectTransform container;
	private List<RectTransform> items;

	private float itemWidth;
	private int currrentItemIndex;
	private float startDragPosition;
	private float swipeStartTime;
	private float targetX;


	private void OnEnable() {
		SubscribeBtnsEvent();
	}

	private void SubscribeBtnsEvent() {
		if (leftBtn)
			leftBtn.onClick.AddListener(SlideLeft);
		if (rightBtn)
			rightBtn.onClick.AddListener(SlideRight);
	}

	private void SlideLeft() {
		StopCoroutine("SmoothLerp");
		currrentItemIndex = GetLeftItemIndex();
		targetX = GetItemPositionX(currrentItemIndex, itemWidth);
		StartCoroutine("SmoothLerp");
	}

	private void SlideRight() {
		StopCoroutine("SmoothLerp");
		currrentItemIndex = GetRightItemIndex();
		targetX = GetItemPositionX(currrentItemIndex, itemWidth);
		StartCoroutine("SmoothLerp");
	}


	private void Start() {
		if (initializeOnAwake)
			Initialize();
	}

	public void Initialize() {
		canvas = GetComponentInParent<Canvas>();
		mainTransform = GetComponent<RectTransform>();
		scrollRect = GetComponent<ScrollRect>();
		container = GetSetupedContainer();

		items = new List<RectTransform>();
		foreach (Transform child in container)
			items.Add(child.GetComponent<RectTransform>());

		currrentItemIndex = CheckAndGetDefaultIndex(items);
		itemWidth = items[currrentItemIndex].sizeDelta.x;

		PlaceToDefaultPosition(currrentItemIndex, itemWidth);
	}

	private RectTransform GetSetupedContainer() {
		RectTransform content = scrollRect.content;
		content.pivot = new Vector2(0f, 1f);
		return content;
	}

	private int CheckAndGetDefaultIndex(List<RectTransform> _items) {
		if (_items.Count > 0) {
			if (defaultItemIndex >= _items.Count)
				return 0;
			return defaultItemIndex;
		}

		Debug.LogError("No items initialized");
		return -1;
	}

	private void PlaceToDefaultPosition(int index, float _itemWidth) {
		Vector2 targetPosition = container.position;
		targetPosition.x = GetItemPositionX(index, _itemWidth);
		container.position = targetPosition;
		Canvas canvas = GetComponent<Canvas>();
	}

	private float GetItemPositionX(int index, float _itemWidth) {
		return (mainTransform.position.x - ((_itemWidth * index + spacing * index + _itemWidth / 2f) * canvas.scaleFactor));
	}


	public void OnBeginDrag(PointerEventData eventData) {
		StopCoroutine("SmoothLerp");

		startDragPosition = eventData.position.x;
		swipeStartTime = Time.time;
	}


	public void OnEndDrag(PointerEventData eventData) {
		scrollRect.StopMovement();
		TryToSwipe(eventData.position.x);
		StartCoroutine("SmoothLerp");
	}

	private void TryToSwipe(float endPosition) {
		float deltaX = (endPosition - startDragPosition) / Screen.width;

		if (Swiped(deltaX)) {
			if (deltaX < 0)
				currrentItemIndex = GetRightItemIndex();
			else
				currrentItemIndex = GetLeftItemIndex();

		}
		else {
			currrentItemIndex = GetNearestItemIndex();
		}

		targetX = GetItemPositionX(currrentItemIndex, itemWidth);
	}

	private bool Swiped(float deltaX) {
		float deltaTime = Time.time - swipeStartTime;
		return deltaTime < swipeDuration && Mathf.Abs(deltaX) > distanceToSwipe;
	}

	private int GetRightItemIndex() {
		if (currrentItemIndex < container.childCount - 1)
			return currrentItemIndex + 1;
		return currrentItemIndex;
	}

	private int GetLeftItemIndex() {
		if (currrentItemIndex > 0)
			return currrentItemIndex - 1;
		return currrentItemIndex;
	}

	private int GetNearestItemIndex() {
		RectTransform nearestItem = items[currrentItemIndex];
		float minDistance = Mathf.Abs(nearestItem.position.x - mainTransform.position.x);
		foreach (RectTransform item in items) {
			float distance = Mathf.Abs(item.position.x - mainTransform.position.x);
			if (distance < minDistance) {
				nearestItem = item;
				minDistance = distance;
			}
		}
		return items.IndexOf(nearestItem);
	}

	private IEnumerator SmoothLerp() {
		float timer = 0f;
		Vector2 originPosition = container.position;
		Vector2 targetPosition = new Vector2(targetX, originPosition.y);

		while (timer < 1f) {
			timer += Time.deltaTime / snapDuration;
			container.position = Vector3.Lerp(originPosition, targetPosition, timer);
			yield return null;
		}

		container.position = targetPosition;
	}

	private void OnDisable() {
		UnsubscribeOnBntsEvents();
	}

	private void UnsubscribeOnBntsEvents() {
		if (leftBtn)
			leftBtn.onClick.RemoveListener(SlideLeft);
		if (rightBtn)
			rightBtn.onClick.RemoveListener(SlideRight);
	}

	public Transform GetCurrentItem() {
		return items[currrentItemIndex];
	}
}