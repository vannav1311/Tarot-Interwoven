using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class IntroLogo : MonoBehaviour
{
    public Image logoImage;
    public float fadeDuration = 1.2f;
    public float showTime = 0.5f;

    void Start()
    {
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        // Logo bắt đầu trong suốt
        Color c = logoImage.color;
        c.a = 0;
        logoImage.color = c;

        // Fade in (hiện dần)
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            c.a = t / fadeDuration;
            logoImage.color = c;
            yield return null;
        }
        c.a = 1;
        logoImage.color = c;

        // Giữ logo trong một khoảng thời gian
        yield return new WaitForSeconds(showTime);

        // Fade out (tối dần)
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            c.a = 1 - t / fadeDuration;
            logoImage.color = c;
            yield return null;
        }

        // Khi xong thì vào scene kế tiếp (ví dụ: MainMenu)
        SceneManager.LoadScene("2.StartScene");
    }
}
