class Occ {
    constructor() {
        console.log("**** construct Occ");

        this.__secondsToWaitAfterBlockReach = 10; // seconds

        this.__divRootClass = "locomotiveInfo";
        this.__divFromClass = "locomotiveInfoFrom";
        this.__divFinalClass = "locomotiveInfoFinal";
        this.__dataOidName = "locoid";
        this.__divBlockedClass = "isblocked";

        this.__borderPaddingLeft = 0;
        this.__borderPaddingTop = 0;
        this.__borderPaddingRight = 4;

        this.__initEventHandling();
    }

    __initEventHandling() {
        this.__events = new Events();
    }

    on(eventName, callback) {
        this.__events.on(eventName, callback);
    }

    __trigger(eventName, dataObject) {
        this.__events.triggerHandler(eventName, {
            sender: this,
            data: dataObject
        });
    }

    /**
     * Searches and returns the locomotive information
     * of the main info container, i.e. the fromBlock-info.
     * @param {any} locOid
     */
    getLocomotiveInfo(locOid) {
        const self = this;
        const locInfos = $('div.' + this.__divRootClass);
        if (typeof locInfos === "undefined") return null;
        if (locInfos == null) return null;
        if (locInfos.length === 0) return null;

        let i;
        const iMax = locInfos.length;
        for (i = 0; i < iMax; ++i) {
            const itmInfo = locInfos[i];
            if (itmInfo == null) continue;

            const dataOid = $(itmInfo).data(this.__dataOidName);
            if (typeof dataOid === "undefined") continue;
            if (dataOid == null) continue;

            if (dataOid === locOid)
                return itmInfo;
        }

        return null;
    }

    __generateLocomotiveInfoIdentifiers(objectId) {
        const self = this;
        return {
            "root": "locomotiveInfo" + objectId,
            "rootFinal": "locomotiveInfoFinal" + objectId,
            "image": "locomotiveInfoImage" + objectId,
            "imageFinal": "locomotiveInfoImageFinal" + objectId
        };
    }

    __showErrorPopup(message, targetEl, offsetX = 5, offsetY = 5) {
        const elPop = $('#routePopUp');
        let left = parseInt(targetEl.css("left").replace("px", ""));
        left -= offsetX;
        let top = parseInt(targetEl.css("top").replace("px", ""));
        top -= offsetY;
        elPop.css({
            "left": left + "px",
            "top": top + "px",
            "background-color": "rgba(255, 0, 0, 0.6)",
            "position": "absolute",
            "font-size": "10px",
            "font-weight": "bold",
            "border-radius": "5px",
            "padding": "2px",
            "color": "white"
        });
        elPop.addClass("noselect");
        elPop.html(message);
        elPop.fadeIn("fast");
        setTimeout(function () { elPop.fadeOut("slow"); }, 2000);
    }

    __createLocomotiveInfo(locData) {
        const self = this;
        const ids = self.__generateLocomotiveInfoIdentifiers(locData.objectId);

        let locomotiveInfo = $('<div>', {})
            .css({
                "z-index": 50,
                "position": "absolute",
                "overflow": "hidden",
                "height": "28px",
                "max-height": "28px",
                "line-height": "28px",
                "display": "inline-block",
                "vertical-align": "middle",
                "font-weight": "bold",
                "cursor": "pointer",
                "text-align": "center"
            })
            .attr("id", ids.root)
            .data(this.__dataOidName, locData.objectId);
        locomotiveInfo.attr("unselectable", "on");
        locomotiveInfo.attr("onselectstart", "return false;");
        locomotiveInfo.attr("onmousedown", "return false;");
        locomotiveInfo.addClass(this.__divRootClass);
        locomotiveInfo.addClass(this.__divFromClass);

        const locLabel = $('<div>')
            .css({
                "position": "absolute",
                "top": "0",
                "left": "0"
            })
            .addClass("locInfoLabel")
            .html(locData.name);
        locLabel.appendTo(locomotiveInfo);

        let locLockStateCtrl = $('<i>').css({
            display: "none",
            position: "absolute",
            right: "1px",
            top: "18px",
            color: "red",
            "font-size": "0.6rem"
        })
            .addClass("fas fa-lock")
            .addClass("lockState");
        locLockStateCtrl.appendTo(locomotiveInfo);

        let locIsStoppedCtrl = $('<i>').css({
            display: "none",
            position: "absolute",
            left: "1px",
            top: "18px",
            color: "red",
            "font-size": "0.6rem"
        })
            .addClass("fas fa-hand-paper")
            .addClass("isStoppedState");
        locIsStoppedCtrl.appendTo(locomotiveInfo);

        let locEnterSideCtrl = $('<i>').css({
            display: "none"
        })
            .addClass("fas fa-long-arrow-alt-right")
            .addClass("enterSide");
        locEnterSideCtrl.appendTo(locomotiveInfo);

        let locSpeedInfoCtrl = $('<div>').css({
            position: "absolute",
            right: "1px",
            bottom: "-10px",
            color: "black",
            "font-size": "0.45rem",
            "letter-spacing": "-1px"
        })
            .html("-.-")
            .addClass("speedInfo");
        locSpeedInfoCtrl.appendTo(locomotiveInfo);

        //
        // final locomotive info
        //
        let locomotiveInfoFinal = locomotiveInfo.clone();
        locomotiveInfoFinal.attr("id", ids.rootFinal);
        locomotiveInfoFinal.data(this.__dataOidName, locData.objectId);
        locomotiveInfoFinal.removeClass(this.__divFromClass);
        locomotiveInfoFinal.addClass(this.__divFinalClass);
        const locImg3 = locomotiveInfoFinal.find('img');
        locImg3.attr("id", ids.imageFinal);
        locomotiveInfoFinal.dblclick(function () {
            self.__trigger("resetAssignment",
                {
                    mode: 'resetAssignment',
                    submode: 'final',
                    oid: locData.objectId
                });
        });
        locomotiveInfoFinal.on("contextmenu", function (event) {
            event.preventDefault();

            new Contextual({
                isSticky: false,
                items: [
                    {
                        cssIcon: 'fas fa-broom',
                        enabled: true,
                        label: 'Remove Assignment', onClick: () => {
                            console.log("OID: " + locData.objectId);
                            self.__trigger("resetAssignment",
                                {
                                    mode: 'resetAssignment',
                                    submode: 'final',
                                    oid: locData.objectId
                                });
                        }
                    }
                ]
            });
        });

        //
        // add wait countdown to fromInfo
        //
        let locCountdown = $('<div>').css({
            "display": "none",
            "text-align": "center",
            "position": "absolute",
            "left": "0px",
            "top": "0px",
            "color": "black",
            "background-color": "lightgray",
            "font-size": "1.0rem",
            "width": locomotiveInfo.css("width"),
            "height": locomotiveInfo.css("height")
        })
            .addClass("countdown")
            .html('<i class="fas fa-history" style="padding-right: 5px;"></i><span class="countdown">...</span>');
        locCountdown.appendTo(locomotiveInfo);

        locomotiveInfo.draggable({
            // ui: helper, position, offset
            opacity: 0.7,
            helper: "clone",
            start: function (ev, ui) {
                const ctrl = self.__getPlanfieldClickControlOfPosition();
                if (ctrl === null) return;
                self.__dragStartBlockElement = ctrl;
            },

            stop: function (ev, ui) {
                var reset = false;

                try {

                    const targetEl = self.__getPlanfieldClickControlOfPosition();
                    if (typeof targetEl === "undefined" || targetEl === null) throw "resetAssignment";
                    if (!targetEl.hasClass("ctrlItemBlock")) throw "resetAssignment";
                    if (targetEl.hasClass(this.__divBlockedClass)) throw "reset";

                    const startEl = self.__dragStartBlockElement;
                    const startId = startEl.attr("id");
                    const targetId = targetEl.attr("id");
                    if (startId === targetId) throw "reset";

                    const startElData = getThemeJsonData(startEl);
                    const targetElData = getThemeJsonData(targetEl);

                    if (!isBlock(targetElData.editor.themeId)) throw "reset";

                    //
                    // check if AutoMode is activated
                    //
                    if (window.__autoModeState === false) {
                        self.__showErrorPopup(
                            "AutoMode is disabled.",
                            targetEl,
                            0,
                            0
                        );
                        return;
                    }

                    //
                    // check if a route exist from start to target
                    // otherwise deny assignment and show an error message
                    //
                    const from0 = startElData.identifier + "[+]";
                    const from1 = startElData.identifier + "[-]";
                    const to0 = targetElData.identifier + "[+]";
                    const to1 = targetElData.identifier + "[-]";

                    const routeName0 = from0 + "_" + to0;
                    const routeName1 = from0 + "_" + to1;
                    const routeName2 = from1 + "_" + to0;
                    const routeName3 = from1 + "_" + to1;

                    let routeExists = false;
                    for (let ii = 0; ii < window.routes.length; ++ii) {
                        const r = window.routes[ii];
                        const name = r.name;
                        if (name === routeName0) routeExists = true;
                        else if (name === routeName1) routeExists = true;
                        else if (name === routeName2) routeExists = true;
                        else if (name === routeName3) routeExists = true;
                    }

                    if (routeExists === false) {
                        self.__showErrorPopup(
                            "Route from "
                            + startElData.identifier
                            + " to "
                            + targetElData.identifier
                            + " does not exist.",
                            targetEl
                        );
                    } else {

                        //
                        // route exists, assign locomotive to target
                        //
                        self.__trigger("gotoBlock",
                            {
                                mode: 'gotoBlock',
                                oid: locData.objectId,
                                fromBlock: startElData.coord,
                                toBlock: targetElData.coord
                            });
                    }

                } catch (err) {
                    if (err === "reset") {
                        reset = true;
                    } else if (err === "resetAssignment") {

                        locomotiveInfo.hide();
                        locomotiveInfoFinal.hide();

                        self.__trigger("resetAssignment",
                            {
                                mode: 'resetAssignment',
                                oid: locData.objectId
                            });
                    }
                } finally {
                    if (reset) {
                        // TBD
                    }

                    self.__dragStartBlockElement = null;
                }
            }
        });

        /**
         * add loc control hover
         */
        const tplLocControl = $('div[id=tplLocomotiveInfoCtrl]');
        var locControl = tplLocControl.find('div.locInfoCtrl').clone();
        locControl.css({ "display": "none" });
        locControl.attr("id", 'locCtrl_' + locData.objectId);

        locControl.find('#locSpeedPlus').click(function () {
            self.__trigger('speedChanged',
                {
                    oid: locData.objectId,
                    speedstep: "++",
                    timestamp: Date.now()
                });
        });
        locControl.find('#locSpeedMinus').click(function () {
            self.__trigger('speedChanged',
                {
                    oid: locData.objectId,
                    speedstep: "--",
                    timestamp: Date.now()
                });
        });
        locControl.find('#locSpeedStop').click(function () {
            self.__trigger('speedChanged',
                {
                    oid: locData.objectId,
                    speedstep: "level0",
                    timestamp: Date.now()
                });
        });
        locControl.find('#locOpenDialog').click(function () {
            const oid = locData.objectId;
            self.__trigger('openLocomotiveControl', {
                oid: oid
            });
        });
        locControl.find('#locBackward').click(function () {
            self.__trigger('directionChanged',
                {
                    oid: locData.objectId,
                    force: "backward"
                });
        });
        locControl.find('#locForward').click(function () {
            self.__trigger('directionChanged',
                {
                    oid: locData.objectId,
                    force: "forward"
                });
        });

        const allBtns = locControl
            .find('.locSpeedSteps')
            .find('button')
            .not("button[data-function]");

        allBtns.each(function () {
            $(this).button();
            if ($(this).data("speedstep")) {
                $(this).click(function (ev) {
                    const speedstep = $(ev.currentTarget).data("speedstep");
                    self.__trigger('speedChanged',
                        {
                            oid: locData.objectId,
                            speedstep: speedstep,
                            timestamp: Date.now()
                        });
                });
            }
        });

        const __fncShowLocHover = function (locinfo) {
            clearTimeout(self.__timeoutBeforeHide);
            self.__timeoutBeforeHide = 0;
            if (self.__timeoutBeforeShow > 0) return;
            if (self.isLocomotiveControlHoverShown(this)) return;
            // hide all other controls
            $('.locInfoCtrl').each(function () { $(this).hide(); });
            self.__timeoutBeforeShow = setTimeout(function () {
                self.showLocomotiveControlHover($(locinfo));
            }, 250);
        }

        const __fncHideLocHover = function (locinfo) {
            clearTimeout(self.__timeoutBeforeShow);
            self.__timeoutBeforeShow = 0;
            self.__timeoutBeforeHide = setTimeout(function () {
                self.hideLocomotiveControlHover($(locinfo));
            }, 250);
        }

        //
        // add locomotive hover control to starting block 
        //
        locomotiveInfo.mouseover(function () {
            __fncShowLocHover(this);
        });
        locomotiveInfo.mouseleave(function () {
            __fncHideLocHover(this);
        });

        //
        // add locomotive hover control to final block
        //
        locomotiveInfoFinal.mouseover(function () {
            __fncShowLocHover(this);
        });
        locomotiveInfoFinal.mouseleave(function () {
            __fncHideLocHover(this);
        });

        locomotiveInfo.on("dblclick", function (event) {
            event.preventDefault();
            const oid = locomotiveInfo.data("oid");
            self.__saveLocomotiveStart(oid, true);
        });

        locomotiveInfo.on("contextmenu", function (event) {
            event.preventDefault();

            const oid = locomotiveInfo.data("oid");
            const locData = self.__getLocomotiveOfRecentData(oid);

            /**
             * EnterSide
             */
            let cssPlusIcon = '';
            let cssMinusIcon = '';
            if (locData.EnterBlockSide === "'+' Side") cssPlusIcon = 'fas fa-check';
            else if (locData.EnterBlockSide === "'-' Side") cssMinusIcon = 'fas fa-check';
            else cssPlusIcon = 'fas fa-check';

            /**
             * Locking
             */
            // <i class="fas fa-lock-open"></i>
            // <i class="fas fa-lock"></i>
            let cssLockIcon = '';
            let cssLockLabel = '';
            if (locData.IsLocked === true) {
                cssLockLabel = "Unlock Locomotive";
                cssLockIcon = 'fas fa-lock';
                locomotiveInfo.data("isLocked", true);
            }
            else if (locData.IsLocked === false) {
                cssLockLabel = "Lock Locomotive";
                cssLockIcon = 'fas fa-lock-open';
                locomotiveInfo.data("isLocked", false);
            } else {
                cssLockLabel = "Unlock Locomotive";
                cssLockIcon = 'fas fa-lock';
                locomotiveInfo.data("isLocked", true);
            }
            const currentLock = locomotiveInfo.data("isLocked");

            new Contextual({
                isSticky: false,
                items: [
                    {
                        cssIcon: 'fas fa-play',
                        enabled: window.__autoModeState,
                        label: 'Start', onClick: () => {
                            self.__saveLocomotiveStart(oid, true);
                        }
                    },
                    {
                        cssIcon: 'fas fa-stop',
                        enabled: window.__autoModeState,
                        label: 'Finalize', onClick: () => {
                            self.__stopWaitCounter(oid);
                            self.__saveLocomotiveFinalize(oid, true);
                        }
                    },
                    { type: 'seperator' },
                    {
                        cssIcon: cssPlusIcon, label: 'Enter "+"', onClick: () => {
                            locomotiveInfo.data("enterSide", "Plus");
                            self.__saveLocomotiveEnterSide(oid, "'+' Side");
                        }
                    },
                    {
                        cssIcon: cssMinusIcon, label: 'Enter "-"', onClick: () => {
                            locomotiveInfo.data("enterSide", "Minus");
                            self.__saveLocomotiveEnterSide(oid, "'-' Side");
                        }
                    },
                    { type: 'seperator' },
                    {
                        cssIcon: cssLockIcon, label: cssLockLabel, onClick: () => {
                            locomotiveInfo.data("isLocked", !currentLock);
                            self.__saveLocomotiveLock(oid, !currentLock);
                        }
                    }
                ]
            });
        });

        const bodyCtrl = $('body');
        locControl.appendTo(bodyCtrl);
        locomotiveInfo.appendTo(bodyCtrl);
        locomotiveInfoFinal.appendTo(bodyCtrl);

        self.__updateStateVisualization(locData.objectId);
    }

    __stopWaitCounter(oid) {
        if (typeof oid === "undefined" || oid == null || oid <= 0) return;
        const lbl = $('#locomotiveInfo' + oid + ' div.countdown');
        lbl.hide();
    }

    __updateTimeVisualization(occDataItem) {
        if (typeof occDataItem === "undefined" || occDataItem == null) return;
        const self = this;
        const reachedDtSecs = parseInt(new Date(occDataItem.ReachedTime).getTime() / 1000);
        const walltimeDtSecs = reachedDtSecs + occDataItem.SecondsToWait;

        let currentSecs = parseInt(walltimeDtSecs - (parseInt(new Date().getTime() / 1000)));
        if (currentSecs < 0) currentSecs = 0;

        const hideCounter = currentSecs <= 0;
        const oid = occDataItem.Oid;
        const lbl = $('#locomotiveInfo' + oid + ' div.countdown');

        if (hideCounter === true) {
            lbl.hide();
        } else {
            if (lbl.is(":visible"))
                return;

            lbl.show();

            const lblCnt = lbl.find("span.countdown");
            lblCnt.data("walltime", walltimeDtSecs);
            lblCnt.html(currentSecs + "s");

            var fncCheckTime = (function (ctrl) {
                setTimeout(function () {
                    const lblCnt = ctrl.find("span.countdown");
                    const walltime = parseInt(lblCnt.data("walltime"));
                    if (walltime >= 0) {
                        const secs = parseInt(lblCnt.data("walltime")) - (parseInt(new Date().getTime() / 1000));
                        if (isNaN(secs) || secs < 1) {
                            lblCnt.data("walltime", -1);
                            ctrl.hide();
                            return;
                        }

                        lblCnt.html(secs + "s");
                        fncCheckTime(ctrl);
                    } else {
                        ctrl.hide();
                        return;
                    }
                }, 1000);
            });

            fncCheckTime(lbl);
        }
    }

    __getLocomotiveOfRecentData(oid) {
        if (typeof this.__recentLocomotivesData === "undefined" || this.__recentLocomotivesData == null)
            return null;
        const data = this.__recentLocomotivesData[oid];
        if (typeof data === "undefined" || data == null) return null;
        return data;
    }

    __fncScaleLbl(ctrl, factor) {
        try {
            const lbl = ctrl.find("div.locInfoLabel");
            if (!lbl.is(":visible")) return;
            if (lbl.width() === 0 || lbl.height() === 0)
                return;
            lbl.boxfit();
            const spanLbl = lbl.find('span');
            let currentFontSize = spanLbl.css("font-size");
            currentFontSize = parseInt(currentFontSize.replace("px", ""));
            spanLbl.css("font-size", parseInt(currentFontSize * factor) + "px");
            ctrl.data("fontIsScaled", true);
        } catch (err) {
            // ignore
        }
    }

    __updateStateVisualization(oid) {
        const self = this;
        const ids = self.__generateLocomotiveInfoIdentifiers(oid);
        const locomotiveInfo = $('#' + ids.root);
        if (typeof locomotiveInfo === "undefined" || locomotiveInfo == null || locomotiveInfo.length === 0) {
            return;
        }

        const locData = self.__getLocomotiveOfRecentData(oid);
        if (locData == null) {
            self.__fncScaleLbl(locomotiveInfo, 0.75);
            return;
        }

        const ctrlSpeedInfo = locomotiveInfo.find('.speedInfo');
        const locEcosData = self.__getEcosDataOfLoc(oid);
        if (locEcosData == null) {
            // unavailable speed information
            ctrlSpeedInfo.html("-.-");
        } else {
            // show speed information
            ctrlSpeedInfo.html(locEcosData.speedstep + "|" + locEcosData.speedstepMax);
        }

        const ctrlEnter = locomotiveInfo.find('.enterSide');
        if (typeof ctrlEnter !== "undefined" && ctrlEnter != null && ctrlEnter.length > 0) {
            let enterBlockSide = "";
            if (locData.EnterBlockSide.includes("'+'"))
                enterBlockSide = "plus";
            else if (locData.EnterBlockSide.includes("'-'"))
                enterBlockSide = "minus";

            ctrlEnter.removeAttr('style');

            if (enterBlockSide === "plus") {
                ctrlEnter.css({
                    "position": "absolute",
                    "right": "2px",
                    "top": "1px",
                    "color": "yellow",
                    "font-size": "0.7rem",
                    "transform": "scaleX(1)"
                });
                ctrlEnter.show();
            } else if (enterBlockSide === "minus") {
                ctrlEnter.css({
                    "position": "absolute",
                    "left": "2px",
                    "top": "1px",
                    "color": "yellow",
                    "font-size": "0.7rem",
                    "transform": "scaleX(-1)"
                });
                ctrlEnter.show();
            } else {
                ctrlEnter.hide();
            }
        }

        const ctrlLock = locomotiveInfo.find('.lockState');
        if (typeof ctrlLock !== "undefined" && ctrlLock != null && ctrlLock.length > 0) {
            //
            // Lock of the Locomotive
            //
            if (locData.IsLocked === false) {
                ctrlSpeedInfo.show();
                ctrlLock.hide();
                self.__fncScaleLbl(locomotiveInfo, 0.70);
            } else {
                ctrlSpeedInfo.hide();
                ctrlLock.show();
                self.__fncScaleLbl(locomotiveInfo, 0.70);
            }
        }

        const ctrlStopped = locomotiveInfo.find('.isStoppedState');
        if (typeof ctrlStopped !== "undefined" && ctrlStopped != null && ctrlStopped.length > 0) {
            //
            // Stop state of the Locomotive
            //
            if (locData.IsStopped === false) {
                ctrlStopped.hide();
            } else {
                ctrlStopped.show();
            }
        }
    }

    __saveLocomotiveStart(oid, state) {
        this.__trigger('setting',
            {
                'mode': 'locomotive',
                'cmd': 'start',
                'value': { oid: oid, state: state }
            });
    }

    __saveLocomotiveFinalize(oid, state) {
        this.__trigger('setting',
            {
                'mode': 'locomotive',
                'cmd': 'finalize',
                'value': { oid: oid, state: state }
            });
    }

    __saveLocomotiveLock(oid, lockState) {
        this.__trigger('setting',
            {
                'mode': 'locomotive',
                'cmd': 'lock',
                'value': {
                    oid: oid,
                    locked: lockState
                }
            });
    }

    __saveLocomotiveEnterSide(oid, enterSide) {
        this.__trigger('setting',
            {
                'mode': 'locomotive',
                'cmd': 'locomotiveData',
                'value': {
                    oid: oid,
                    checkboxSettings: null, // null means NO CHANGE BY THIS CALL
                    blockEnterSide: enterSide
                }
            });
    }

    isLocomotiveControlHoverShown(locInfoCtrl) {
        const oid = $(locInfoCtrl).data(this.__dataOidName);
        const locController = $('#locCtrl_' + oid);
        return locController.is(":visible");
    }

    showLocomotiveControlHover(locInfoCtrl) {
        const self = this;
        const oid = $(locInfoCtrl).data(this.__dataOidName);
        const locController = $('#locCtrl_' + oid);
        locController.show();

        const cc = locInfoCtrl.get(0);
        const left = cc.offsetLeft;
        const top = cc.offsetTop;
        const locInfoHeight = locInfoCtrl.get(0).getBoundingClientRect().height;

        const rect = locController.get(0).getBoundingClientRect();
        const h = rect.height;
        const w = rect.width;

        const offsetY = parseInt(h - locInfoHeight) / 2;

        let ypos = (top - offsetY);
        if (ypos < 0) ypos = 5;

        locController.css({
            "z-index": 150,
            top: ypos + "px",
            left: (left - w + 1) + "px"
        });

        locController.mousemove(function () {
            clearTimeout(self.__timeoutBeforeHide);
            self.__timeoutBeforeHide = 0;
        });

        locController.mouseleave(function () {
            setTimeout(function () {
                locController.hide();
            }, 125);
        });
    }

    hideLocomotiveControlHover(locInfoCtrl) {
        const oid = $(locInfoCtrl).data(this.__dataOidName);
        const locController = $('#locCtrl_' + oid);
        locController.hide();
    }

    loadLocomotives(locomotives) {
        const self = this;

        self.__recentLocomotives = locomotives;

        for (let j = 0; j < locomotives.length; ++j) {
            if (locomotives[j] === null) continue;
            const loc = locomotives[j];
            const locOid = loc.objectId;

            // check for already available locomotive info
            const existingLocInfo = self.getLocomotiveInfo(locOid);
            if (existingLocInfo != null)
                continue;

            self.__createLocomotiveInfo(loc);
        }
    }

    resetAssignment() {
        console.log("reset assignment");
    }

    /**
     * Returns an jquery-instance or null.
     * @param {any} blockName
     */
    __getBlock(blockName) {
        const self = this;
        const allBlocks = $('div.ctrlItemBlock');
        for (let i = 0; i < allBlocks.length; ++i) {
            const block = allBlocks[i];
            if (typeof block === "undefined" || block == null) continue;
            const data = getThemeJsonData($(block));
            if (typeof data === "undefined" || data == null) continue;
            if (data.identifier === blockName)
                return $(block);
        }
        return null;
    }

    __applyInfoToBlock(infoInstance, targetBlock, occDataItem) {
        if (typeof occDataItem === "undefined" || occDataItem == null)
            occDataItem = null;
        const self = this;
        if (typeof targetBlock === "undefined" || targetBlock == null)
            return;
        const ctrlPostion = targetBlock.get(0).getBoundingClientRect();
        const ctrlWidth = ctrlPostion.width;

        const cssTop = (ctrlPostion.top + this.__borderPaddingTop) + "px";
        const cssLeft = (ctrlPostion.left + this.__borderPaddingLeft) + "px";
        let cssWidth = 0;
        let cssMaxWidth = 0;

        if (ctrlWidth <= 64) {
            cssWidth = (2 * constItemWidth - this.__borderPaddingRight) + "px";
            cssMaxWidth = (2 * constItemWidth - this.__borderPaddingRight) + "px";
        } else {
            cssWidth = (4 * constItemWidth - this.__borderPaddingRight) + "px";
            cssMaxWidth = (4 * constItemWidth - this.__borderPaddingRight) + "px";
        }

        infoInstance.css({
            "top": cssTop,   // if changes, the drop pos must be changed as well !!
            "left": cssLeft,
            "width": cssWidth,
            "max-width": cssMaxWidth,
            "text-align": "left"
        });

        let lbl = infoInstance.find("div.locInfoLabel");
        if (typeof lbl !== "undefined" && lbl != null && lbl.length > 0) {
            lbl.css("width", cssWidth);
            lbl.css("max-width", cssMaxWidth);
            lbl.css("height", constItemHeight);
        }
        let lblCounter = infoInstance.find("div.countdown");
        if (typeof lblCounter !== "undefined" && lblCounter != null && lblCounter.length > 0) {
            lblCounter.css("width", cssWidth);
            lblCounter.css("max-width", cssMaxWidth);
            lblCounter.css("height", constItemHeight);
        }

        if (occDataItem != null) {
            infoInstance.data("oid", occDataItem.Oid);
            self.__updateStateVisualization(occDataItem.Oid);
        }

        // used for drag&drop
        targetBlock.addClass(self.__divBlockedClass);
    }

    handleData(jsonData) {
        const self = this;
        const occData = jsonData.data; // array

        var allOids = [];
        let i;
        const iMax = occData.length;
        for (i = 0; i < iMax; ++i) {
            const occDataItem = occData[i];
            allOids.push(occDataItem.Oid);
        }

        const allInfos = $('div.locomotiveInfo');
        if (iMax === 0) {
            allInfos.each(function () { $(this).hide(); });
        } else {
            allInfos.each(function () {
                const info = $(this);
                const infoOid = info.data(this.__dataOidName);
                if (!allOids.includes(infoOid))
                    info.hide();
            });
        }

        for (i = 0; i < iMax; ++i) {
            const occDataItem = occData[i];
            // FinalBlock
            // FromBlock
            // Oid
            // EnterSide
            // ReachedTime
            // EarliestStartTime

            if (typeof occDataItem === "undefined") continue;
            if (occDataItem == null) continue;

            const ids = self.__generateLocomotiveInfoIdentifiers(occDataItem.Oid);
            const infoInstance = $('div#' + ids.root);
            const infoInstanceFinal = $('div#' + ids.rootFinal);

            const fromBlockId = occDataItem.FromBlock;
            const finalBlockId = occDataItem.FinalBlock;

            if (typeof fromBlockId === "undefined" || fromBlockId == null || fromBlockId.length === 0) {
                infoInstance.hide();
            } else {
                const fromBlock = self.__getBlock(fromBlockId);
                infoInstance.show();
                self.__applyInfoToBlock(infoInstance, fromBlock, occDataItem);
            }

            if (typeof finalBlockId === "undefined" || finalBlockId == null || finalBlockId.length === 0) {
                infoInstanceFinal.hide();
            } else {
                const finalBlock = self.__getBlock(finalBlockId);
                infoInstanceFinal.show();
                self.__applyInfoToBlock(infoInstanceFinal, finalBlock);
                self.__fncScaleLbl(infoInstanceFinal, 0.9);
            }

            const finalEntered = occDataItem.FinalEntered;
            if (typeof finalEntered !== "undefined" && finalEntered != null) {
                if (finalEntered === true) {
                    infoInstanceFinal.find("div.locInfoLabel").addClass("locomotiveInfoFinalEntering");
                } else {
                    infoInstanceFinal.find("div.locInfoLabel").removeClass("locomotiveInfoFinalEntering");
                }
            }

            self.__updateTimeVisualization(occDataItem);
        }
    }

    updateOccWithLocomotiveData(locomotivesData) {
        const self = this;
        const data = locomotivesData.data;

        this.__recentLocomotivesData = data;

        for (let key in data) {
            if (data.hasOwnProperty(key)) {
                const oid = parseInt(key);

                self.__updateStateVisualization(oid);
            }
        }
    }

    updateEcosDirection(ecosLocomotives) {
        const self = this;
        if (typeof ecosLocomotives === "undefined") return;
        if (ecosLocomotives == null) return;

        let i = 0;
        const iMax = ecosLocomotives.length;
        for (; i < iMax; ++i) {
            const ecosLoc = ecosLocomotives[i];
            const locoid = ecosLoc.objectId;

            const locControl = $('#locCtrl_' + locoid);
            const cmdBackward = locControl.find('#locBackward');
            const cmdForward = locControl.find('#locForward');

            cmdBackward.css({ "color": "black" });
            cmdForward.css({ "color": "black" });

            const locEcosData = self.__getEcosDataOfLoc(locoid);
            if (locEcosData == null) return;

            if (locEcosData.direction === 0) // forward
                cmdForward.css({ "color": "green" });
            else if (locEcosData.direction === 1) // backward
                cmdBackward.css({ "color": "green" });
        }
    }

    updateEcosSpeedInfo(ecosLocomotives) {
        const self = this;
        if (typeof ecosLocomotives === "undefined") return;
        if (ecosLocomotives == null) return;

        let i = 0;
        const iMax = ecosLocomotives.length;
        for (; i < iMax; ++i) {
            const ecosLoc = ecosLocomotives[i];
            const locoid = ecosLoc.objectId;

            const ids = self.__generateLocomotiveInfoIdentifiers(locoid);
            const locomotiveInfo = $('#' + ids.root);
            if (typeof locomotiveInfo === "undefined" || locomotiveInfo == null || locomotiveInfo.length === 0) {
                continue;
            }

            const ctrlSpeedInfo = locomotiveInfo.find('.speedInfo');
            const locEcosData = self.__getEcosDataOfLoc(locoid);
            const currentHtml = ctrlSpeedInfo.html();
            if (locEcosData == null) {
                // unavailable speed information
                if (currentHtml === "-.-")
                    ;
                else
                    ctrlSpeedInfo.html("-.-");
            } else {
                // show speed information
                const newHtml = locEcosData.speedstep + "|" + locEcosData.speedstepMax;
                if (newHtml === currentHtml)
                    ;
                else
                    ctrlSpeedInfo.html(locEcosData.speedstep + "|" + locEcosData.speedstepMax);
            }
        }
    }

    __getEcosDataOfLoc(locoid) {
        const self = this;
        if (typeof window.ecosData === "undefined") return null;
        if (window.ecosData == null) return null;
        if (typeof window.ecosData.locomotives === "undefined") return null;
        if (window.ecosData.locomotives == null) return null;
        let i = 0;
        const iMax = window.ecosData.locomotives.length;
        for (; i < iMax; ++i) {
            const locIt = window.ecosData.locomotives[i];
            if (typeof locIt === "undefined") continue;
            if (locIt == null) continue;
            if (locIt.objectId === locoid)
                return locIt;
        }
        return null;
    }

    __getPlanfieldClickControlOfPosition() {
        const c = window.planField.planfieldElement.get(0);
        const x = event.clientX - c.offsetLeft;
        const y = event.clientY - c.offsetTop;
        const ctrls = $('div.ctrlItem[id]');
        const ctrl = getCtrlOfPosition(x, y, ctrls);
        return ctrl;
    }
}