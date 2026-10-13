# VideoEmbed

Click-to-play YouTube: a 16:9 poster with a play button, swapped for the `youtube-nocookie.com`
player only when clicked (or activated with Enter/Space). No YouTube code loads until then. From
`file://`, where the embed is blocked, it opens the video on YouTube instead.

```html
<div class="ma-video" data-yt="NG4IISU4-iQ" role="button" tabindex="0" aria-label="Play: Title">
  <img alt="" src="poster.jpg" width="800" height="450" loading="lazy" decoding="async">
  <span class="ma-video__play" aria-hidden="true"></span>
</div>
```

Load `video-embed.css`, and `video-embed.js` after the markup. The base has no outer margin; set
spacing in the page's CSS. Consumers: mindatticcares.com.
