/* MindAttic.Components - VideoEmbed/video-embed.js
   Markup: a div.ma-video with data-yt="VIDEO_ID", role="button", tabindex="0" and an aria-label,
   holding the poster image (with width/height) and an empty span.ma-video__play (aria-hidden).
   Click (or Enter/Space) swaps the poster for the youtube-nocookie player; from file:// (where
   the embed is blocked) it opens the video on YouTube instead. */
(function () {
  document.querySelectorAll('.ma-video[data-yt]').forEach(function(box){
    function play(){
      var id = box.getAttribute('data-yt');
      if (window.location.protocol === 'file:') {
        window.open('https://www.youtube.com/watch?v=' + id, '_blank', 'noopener');
        return;
      }
      var iframe = document.createElement('iframe');
      iframe.src = 'https://www.youtube-nocookie.com/embed/' + id + '?autoplay=1&rel=0';
      iframe.title = 'YouTube video';
      iframe.allow = 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share';
      iframe.setAttribute('allowfullscreen', '');
      box.innerHTML = '';
      box.appendChild(iframe);
      box.style.cursor = 'default';
      // Once playing the box is no longer a button: another Enter/Space on it (focus stays there
      // after a keyboard start) would rebuild the iframe and restart the video. Hand focus to the
      // player instead.
      box.removeEventListener('click', play);
      box.removeEventListener('keydown', onKey);
      box.removeAttribute('role');
      box.removeAttribute('tabindex');
      iframe.focus();
    }
    function onKey(e){
      if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); play(); }
    }
    box.addEventListener('click', play);
    box.addEventListener('keydown', onKey);
  });
})();
