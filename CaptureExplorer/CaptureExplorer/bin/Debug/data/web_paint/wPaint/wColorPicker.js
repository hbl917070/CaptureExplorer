/******************************************
 * Websanova.com
 *
 * Resources for web entrepreneurs
 *
 * @author          Websanova
 * @copyright       Copyright (c) 2012 Websanova.
 * @license         This wChar jQuery plug-in is dual licensed under the MIT and GPL licenses.
 * @link            http://www.websanova.com
 * @github			http://github.com/websanova/wColorPicker
 * @version         Version 1.3.2
 *
 ******************************************/
(function($) {
  $.fn.wColorPicker = function(option, settings) {
    if (typeof option === "object") {
      settings = option;
    } else if (typeof option === "string") {
      var values = [];

      var elements = this.each(function() {
        var data = $(this).data("_wColorPicker");

        if (data) {
          if ($.fn.wColorPicker.defaultSettings[option] !== undefined) {
            if (settings !== undefined) {
              data.settings[option] = settings;
            } else {
              values.push(data.settings[option]);
            }
          }
        }
      });

      if (values.length === 1) {
        return values[0];
      }
      if (values.length > 0) {
        return values;
      } else {
        return elements;
      }
    }

    settings = $.extend({}, $.fn.wColorPicker.defaultSettings, settings || {});

    return this.each(function() {
      var elem = $(this);
      var $settings = jQuery.extend(true, {}, settings);

      var cp = new ColorPicker($settings, elem);

      cp.generate();

      cp.appendToElement(elem);

      cp.colorSelect(cp, $settings.initColor);

      elem.data("_wColorPicker", cp);
    });
  };

  $.fn.wColorPicker.defaultSettings = {
    theme: "black", // colors - black, white, cream, red, green, blue, yellow, orange, plum
    opacity: 0.95, // opacity level
    initColor: "#FF0000", // initial colour to set palette to
    onMouseover: null, // function to run when palette color is moused over
    onMouseout: null, // function to run when palette color is moused out
    onSelect: null, // function to run when palette color is selected
    mode: "flat", // flat mode inserts the palette to container, other modes insert button into container - hover, click
    buttonSize: 20, // size of button if mode is ohter than flat
    effect: "none", // none/slide/fade
    showSpeed: 500, // time to run the effects on show
    hideSpeed: 500 // time to run the effects on hide
  };

  /**
   * ColorPicker class definition
   */
  function ColorPicker(settings, elem) {
    this.colorPicker = null;
    this.settings = settings;
    this.$elem = elem;
    this.currentColor = settings.initColor;

    this.height = null; // init this, need to get height/width proper while element is still showing
    this.width = null;
    this.slideTopToBottom = null; // used to assist with sliding in proper direction

    this.customTarget = null;
    this.buttonColor = null;
    this.paletteHolder = null;

    return this;
  }

  ColorPicker.prototype = {
    generate: function() {
      if (this.colorPicker) return this.colorPicker;

      var $this = this;

      var clearFloats = {
        clear: "both",
        height: 0,
        lineHeight: 0,
        fontSize: 0
      };

      //custom colors
      this.customTarget = $('<div class="_wColorPicker_customTarget"></div>');
      this.customInput = $(
        '<input type="text" class="_wColorPicker_customInput" value=""/>'
      )
        .keyup(function(e) {
          var code = e.keyCode ? e.keyCode : e.which;

          var hex = $this.validHex($(this).val());

          $(this).val(hex);

          //auto set color in target if it's valid hex code
          if (hex.length == 7) $this.customTarget.css("backgroundColor", hex);

          if (code == 13) {
            //set color if user hits enter while on input
            $this.colorSelect($this, $(this).val());
            if ($this.buttonColor) $this.hidePalette($this);
          }
        })
        .click(function(e) {
          e.stopPropagation();
        });

      //setup custom area
      var custom = $('<div class="_wColorPicker_custom"></div>')
        .append(
          this.appendColors($('<div class="_wColorPicker_noColor">'), [""], 1)
        )
        .append(this.customTarget)
        .append(this.customInput)
        //clear floats
        .append($("<div></div>").css(clearFloats));

      //grays/simple palette
      var simpleColors = [];
      var simplePalette = this.appendColors(
        $('<div class="_wColorPicker_palette_simple"></div>'),
        simpleColors,
        1
      );

      //colors palette
      /*var mixedColors = [
        "000000",
        "333333",
        "666666",
        "999999",
        "CCCCCC",
        "FFFFFF",
        "FF0000",
        "00FF00",
        "0000FF",
        "FFFF00",
        "00FFFF",
        "FF00FF"
	  ];*/
      var mixedColors = [
        "FFFFFF",
        "000000",
        "FF0000",
        "FFFF00",
        "00FF00",
        "00FFFF",
        "0000FF",
        "FF00FF",
        "C1272D",
        "ED1C24",
        "F15A24",
        "F7931E",
        "FBB03B",
        "FCEE21",
        "D9E021",
        "8CC63F",
        "39B54A",
        "009245",
        "006837",
        "22B573",
        "00A99D",
        "29ABE2",
        "0071BC",
        "2E3192",
        "1B1464",
        "662D91",
        "93278F",
        "9E005D",
        "D4145A",
        "ED1E79",
        "C7B299",
        "998675",
        "736357",
        "534741",
        "C69C6D",
        "A67C52",
        "8C6239",
        "754C24",
        "603813",
        "42210B"
      ];

      var mixedPalette = this.appendColors(
        $('<div class="_wColorPicker_palette_mixed"></div>'),
        mixedColors,
        10
      );

      //palette container
      var bg = $('<div class="_wColorPicker_bg"></div>').css({
        opacity: this.settings.opacity
      });
      var content = $('<div class="_wColorPicker_content"></div>')
        .append(custom)
        .append(simplePalette)
        .append(mixedPalette)
		.append($("<div></div>").css(clearFloats))
		//.append($('<input class="color_tr" type="range" min="0" max="100" step="any" value="50">'));//透明度滑竿

      //put it all together
      this.colorPicker = $('<div class="_wColorPicker_holder"></div>')
        .click(function(e) {
          e.stopPropagation();
        })
        .append(
          $('<div class="_wColorPicker_outer"></div>').append(
            $('<div class="_wColorPicker_inner"></div>')
              .append(bg)
              .append(content)
          )
        )
        .addClass("_wColorPicker_" + this.settings.theme);

      return this.colorPicker;
    },

    appendColors: function($palette, colors, lineCount) {
      var counter = 1;
      var $this = this;

      for (index in colors) {
        $palette.append(
          $(
            '<div id="_wColorPicker_color_' +
              counter +
              '" class="_wColorPicker_color _wColorPicker_color_' +
              counter +
              '"></div>'
          )
            .css("backgroundColor", "#" + colors[index])
            .click(function() {
              $this.colorSelect($this, $(this).css("backgroundColor"));
            })
            .mouseout(function(e) {
              $this.colorHoverOff($this, $(this));
            })
            .mouseover(function() {
              $this.colorHoverOn($this, $(this));
            })
        );

        if (counter == lineCount) {
          $palette.append(
            $("<div></div>").css({
              clear: "both",
              height: 0,
              fontSize: 0,
              lineHeight: 0,
              marginTop: -1
            })
          );
          counter = 0;
        }

        counter++;
      }

      return $palette;
    },

    colorSelect: function($this, color) {
      color = $this.toHex(color);

	  //color= "rgba(255,60,255,0.01)";

      $this.customTarget.css("backgroundColor", color);

      $this.currentColor = color;
      $this.customInput.val(color);

      if ($this.settings.onSelect) $this.settings.onSelect.apply(this, [color]);

      if ($this.buttonColor) {
        $this.buttonColor.css("backgroundColor", color);

        //透明顏色的話，就讓背景變成斜線
        if (color == "transparent") {
          $this.buttonColor.css(
            "backgroundImage",
            "url(data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAB4AAAAeCAYAAAA7MK6iAAAACXBIWXMAAAsSAAALEgHS3X78AAAAN0lEQVRIie3NsQ0AIAwEsR+V/ZcIAokNotD4miud6SpZdTfYQ8/HVCgUCoVCoVAoFApt6AuaZAMHwYtLhlklPwAAAABJRU5ErkJggg==)"
          );
        } else {
          $this.buttonColor.css("backgroundImage", "");
        }

        $this.hidePalette($this);
      }
    },

    colorHoverOn: function($this, $element) {
      $element
        .parent()
        .children("active")
        .removeClass("active");
      $element
        .addClass("active")
        .next()
        .addClass("activeLeft");
      $element
        .nextAll("." + $element.attr("id") + ":first")
        .addClass("activeTop");

      var color = $this.toHex($element.css("backgroundColor"));

      $this.customTarget.css("backgroundColor", color);

      $this.customInput.val(color);

      if ($this.settings.onMouseover)
        $this.settings.onMouseover.apply(this, [color]);
    },

    colorHoverOff: function($this, $element) {
      $element
        .removeClass("active")
        .next()
        .removeClass("activeLeft");
      $element
        .nextAll("." + $element.attr("id") + ":first")
        .removeClass("activeTop");

      $this.customTarget.css("backgroundColor", $this.currentColor);
      $this.customInput.val($this.currentColor);

      if ($this.settings.onMouseout)
        $this.settings.onMouseout.apply(this, [$this.currentColor]);
    },

    appendToElement: function($element) {
      var $this = this;

      if ($this.settings.mode == "flat") $element.append($this.colorPicker);
      else {
        //setup button
        $this.paletteHolder = $(
          '<div class="_wColorPicker_paletteHolder"></div>'
        )
          .css({ position: "absolute", overflow: "hidden", width: 1000 })
          .append($this.colorPicker);

        $this.buttonColor = $(
          '<div class="_wColorPicker_buttonColor"></div>'
        ).css({
          width: $this.settings.buttonSize,
          height: $this.settings.buttonSize
        });

        var buttonHolder = $('<div class="_wColorPicker_buttonHolder"></div>')
          .css({ position: "relative" })
          .append(
            $('<div class="_wColorPicker_buttonBorder"></div>').append(
              $this.buttonColor
            )
          )
          .append($this.paletteHolder);

        $element.append(buttonHolder);

        $this.width = $this.colorPicker.outerWidth(true);
        $this.height = $this.colorPicker.outerHeight(true);
        $this.paletteHolder
          .css({ width: $this.width, height: $this.height })
          .hide();

        if ($this.settings.effect == "fade")
          $this.paletteHolder.css({ opacity: 0 });

        //setup events
        if ($this.settings.mode == "hover") {
          buttonHolder.hover(
            function(e) {
              $this.showPalette(e, $this);
            },
            function(e) {
              $this.hidePalette($this);
            }
          );
        } else if ($this.settings.mode == "click") {
          $(document).click(function() {
            if ($this.paletteHolder.hasClass("active"))
              $this.hidePalette($this);
          });

          buttonHolder.click(function(e) {
            e.stopPropagation();
            $this.paletteHolder.hasClass("active")
              ? $this.hidePalette($this)
              : $this.showPalette(e, $this);
          });
        }

        $this.colorSelect($this, $this.settings.initColor);
      }
    },

    showPalette: function(e, $this) {
      var offset = $this.paletteHolder.parent().offset();

      //init some vars
      var left = 0;
      var top = $this.paletteHolder.parent().outerHeight(true);
      $this.slideTopToBottom = top;

      if (
        offset.left - $(window).scrollLeft() + $this.width >
        $(window).width()
      )
        left =
          -1 * ($this.width - $this.paletteHolder.parent().outerWidth(true));
      if (
        offset.top - $(window).scrollTop() + $this.height >
        $(window).height()
      ) {
        $this.slideTopToBottom = 0;
        top = -1 * $this.height;
      }

      $this.paletteHolder.css({ left: left, top: top });

      $this.paletteHolder.addClass("active");

      if ($this.settings.effect == "slide") {
        $this.paletteHolder
          .stop(true, false)
          .css({ height: 0, top: $this.slideTopToBottom == 0 ? 0 : top })
          .show()
          .animate(
            { height: $this.height, top: top },
            $this.settings.showSpeed
          );
      } else if ($this.settings.effect == "fade") {
        $this.paletteHolder
          .stop(true, false)
          .show()
          .animate({ opacity: 1 }, $this.settings.showSpeed);
      } else {
        $this.paletteHolder.show();
      }
    },

    hidePalette: function($this) {
      //need this to avoid the double hide when you click on colour (once on click, once on mouse out) - this way it's only triggered once
      if ($this.paletteHolder.hasClass("active")) {
        $this.paletteHolder.removeClass("active");

        if ($this.settings.effect == "slide") {
          $this.paletteHolder.stop(true, false).animate(
            {
              height: 0,
              top: $this.slideTopToBottom == 0 ? 0 : $this.slideTopToBottom
            },
            $this.settings.hideSpeed,
            function() {
              $this.paletteHolder.hide();
            }
          );
        } else if ($this.settings.effect == "fade") {
          $this.paletteHolder
            .stop(true, false)
            .animate({ opacity: 0 }, $this.settings.hideSpeed, function() {
              $this.paletteHolder.hide();
            });
        } else {
          $this.paletteHolder.hide();
        }
      }
    },

    toHex: function(color) {
      if (color.substring(0, 4) === "rgba") {
        hex = "transparent";
      } else if (color.substring(0, 3) === "rgb") {
        var rgb = color
          .substring(4, color.length - 1)
          .replace(/\s/g, "")
          .split(",");

        for (i in rgb) {
          rgb[i] = parseInt(rgb[i]).toString(16);
          if (rgb[i] == "0") rgb[i] = "00";
        }

        var hex = "#" + rgb.join("").toUpperCase();
      } else {
        hex = color;
      }

      return hex;
    },

    validHex: function(hex) {
      return (
        "#" +
        hex
          .replace(/[^0-9a-f]/gi, "")
          .substring(0, 6)
          .toUpperCase()
      );
    }
  };
})(jQuery);
