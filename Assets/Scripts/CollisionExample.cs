using System.Collections;
using System.Collections.Generic;
using PUCV.PhysicEngine2D;
using UnityEngine;
using CustomCollider2D = PUCV.PhysicEngine2D.CustomCollider2D;

public class CollisionExample : MonoBehaviour, IHasCollider
{
    private SpriteRenderer _spriteRenderer;
    private Coroutine _colorCoroutine;
    private CustomRigidbody2D _rigidbody2D;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rigidbody2D = GetComponent<CustomRigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnInformCollisionEnter2D(CollisionInfo collisionInfo)
    {
        Debug.Log(collisionInfo.otherCollider.name);
        if (_colorCoroutine != null)
        {
            StopCoroutine(_colorCoroutine);
            _spriteRenderer.color = Color.white;
            _colorCoroutine = null;
        }
        _colorCoroutine = StartCoroutine(ChangeColor());

        /*
        if (_rigidbody2D)
        {
            _rigidbody2D.velocity = collisionInfo.contactNormal * _rigidbody2D.velocity.magnitude;
        }
        */
    }
    
    private IEnumerator ChangeColor()
    {
        float duration = 1f;
        float elapsed = 0f;

        Color startColor = Color.red;
        Color endColor = Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _spriteRenderer.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }

        _spriteRenderer.color = endColor;
        _colorCoroutine = null;
    }
}
