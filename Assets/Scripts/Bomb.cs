using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Bomb : MonoBehaviour, IDamageable
{
    public static event EventHandler OnGameOver;

    [SerializeField] GameObject explosionPrefab;

    bool _hit;

    public void Damage()
    {
        if (_hit) return;
        _hit = true;

        foreach (var c in GetComponents<Collider2D>())
            c.enabled = false;

        if (gameObject.TryGetComponent<Image>(out var image))
            image.color = new Color(1, 1, 1, 0);

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        GameAudio.PlayBombHit(transform.position);

        GameSession.LoseLife(1);

        StartCoroutine(ResolveHit());
    }

    IEnumerator ResolveHit()
    {
        yield return new WaitForSecondsRealtime(0.7f);

        if (GameSession.Lives <= 0)
            OnGameOver?.Invoke(this, EventArgs.Empty);

        Destroy(gameObject);
    }
}
