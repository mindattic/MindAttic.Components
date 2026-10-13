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
    }
    box.addEventListener('click', play);
    box.addEventListener('keydown', function(e){
      if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); play(); }
    });
  });
})();
